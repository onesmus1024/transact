# Transact Kubernetes manifests

Plain manifests for deploying Transact to the EKS cluster created by
`aws/compute/eks.yaml`. Public traffic is fronted by an ALB provisioned
by the AWS Load Balancer Controller via the Gateway API
(`gatewayClassName: alb`); IRSA for the controller comes from
`aws/compute/aws-lb-controller.yaml`.

## Layout

```
k8s/
  namespace.yaml                # transact namespace
  api/
    deployment.yaml             # ASP.NET API (Transact.Api)
    service.yaml
    configmap.yaml              # ASPNETCORE_ENVIRONMENT
    cluster-secret-store.yaml   # ESO -> AWS Secrets Manager
    external-secret.yaml        # Renders DB connection string into a K8s Secret
  web/
    deployment.yaml             # Angular SPA served by nginx (Transact.Web)
    service.yaml
  gateway/
    gateway.yaml                # Gateway + HTTPRoutes (alb GatewayClass)
```

## Apply order

```powershell
# 1. Namespace
kubectl apply -f k8s/namespace.yaml

# 2. Replace the image placeholders with real ECR URIs
$ACCOUNT = "<aws-account-id>"
$REGION  = "us-east-1"
$REPO    = "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/transact"
$SHA     = git rev-parse --short HEAD

(Get-Content k8s/api/deployment.yaml) `
  -replace 'REPLACE_ME_API_IMAGE', "$REPO:api-$SHA" |
  kubectl apply -f -

(Get-Content k8s/web/deployment.yaml) `
  -replace 'REPLACE_ME_WEB_IMAGE', "$REPO:web-$SHA" |
  kubectl apply -f -

# 3. Config + DB secret (External Secrets Operator pulls credentials
#    from AWS Secrets Manager; install ESO once per cluster).
kubectl apply -f k8s/api/configmap.yaml

# Substitute the RDS endpoint from the CFN root stack outputs:
$STACK    = "transact-dev-us-east-1-core-01"
$DB_HOST  = aws cloudformation describe-stacks --stack-name $STACK `
            --query "Stacks[0].Outputs[?OutputKey=='DbEndpointAddress'].OutputValue" --output text

(Get-Content k8s/api/external-secret.yaml) `
  -replace 'REPLACE_ME_DB_HOST', $DB_HOST |
  kubectl apply -f -

# ClusterSecretStore is cluster-scoped — apply once per cluster:
kubectl apply -f k8s/api/cluster-secret-store.yaml

# 4. Services
kubectl apply -f k8s/api/service.yaml -f k8s/web/service.yaml

# 5. Gateway + routes (AWS LBC provisions a public ALB)
kubectl apply -f k8s/gateway/gateway.yaml

# 6. Get the assigned ALB DNS name from the Gateway status, then CNAME
#    transact.ryansofttechnologies.com -> this value at Namecheap.
kubectl -n transact get gateway transact -o jsonpath='{.status.addresses[0].value}'
```

## Notes

- Containers run as non-root with `readOnlyRootFilesystem: true`. The
  `Transact.Api` Dockerfile must support a non-root user listening on port
  `8080`; the `Transact.Web` nginx image must be configured to listen on
  `8080` and write caches to `/var/cache/nginx` / `/tmp` (already provided
  via emptyDir volumes).
- DB credentials are pulled from the Secrets Manager secret created by
  `aws/secrets/db-secret.yaml` via External Secrets Operator. The IRSA
  role for the ESO ServiceAccount is provisioned by
  `aws/compute/external-secrets.yaml` (output: `ExternalSecretsRoleArn`).
  Install ESO once per cluster (Helm) and annotate its ServiceAccount
  with that role ARN — see the install snippet below.
- For per-env values (replicas, image tags, environment), wrap these in
  Kustomize overlays or convert to a Helm chart.

## Install External Secrets Operator (once per cluster)

The recommended path is to run the **Bootstrap cluster add-ons** GitHub
Actions workflow ([`.github/workflows/bootstrap.yml`](../.github/workflows/bootstrap.yml)),
which installs both ESO and the AWS Gateway API Controller via Helm and
wires the IRSA role ARNs from the root CFN stack outputs:

```text
Actions ▸ Bootstrap cluster add-ons ▸ Run workflow ▸ environment: dev
```

To install manually instead:

```powershell
$STACK = "transact-dev-us-east-1-core-01"
$ESO_ROLE = aws cloudformation describe-stacks --stack-name $STACK `
            --query "Stacks[0].Outputs[?OutputKey=='ExternalSecretsRoleArn'].OutputValue" --output text

helm repo add external-secrets https://charts.external-secrets.io
helm repo update

helm upgrade --install external-secrets external-secrets/external-secrets `
  --namespace external-secrets --create-namespace `
  --set installCRDs=true `
  --set serviceAccount.annotations."eks\.amazonaws\.com/role-arn"=$ESO_ROLE
```

After ESO is running, apply `k8s/api/cluster-secret-store.yaml` and
`k8s/api/external-secret.yaml`. The operator will create the
`transact-db-connection` Kubernetes Secret automatically and refresh it
every hour.
