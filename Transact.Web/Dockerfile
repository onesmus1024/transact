# syntax=docker/dockerfile:1.7
# ---------- Build stage ----------
FROM node:22-alpine AS build
WORKDIR /src

# Restore deps (cached on package files).
COPY package.json package-lock.json ./
RUN npm ci

# Build the SPA.
COPY . .
RUN npm run build -- --configuration production

# ---------- Runtime stage ----------
FROM nginx:1.27-alpine AS runtime

# Top-level nginx config (pid/temp paths under /tmp for read-only root FS)
# and the per-server reverse-proxy / SPA-fallback config.
COPY nginx/nginx.conf       /etc/nginx/nginx.conf
COPY nginx/default.conf     /etc/nginx/conf.d/default.conf

# Angular build output is written to dist/<project>/browser in v17+.
COPY --from=build /src/dist/Transact.Web/browser /usr/share/nginx/html

# Run as the non-root nginx user shipped in the upstream image (uid 101).
USER 101:101

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/ >/dev/null 2>&1 || exit 1
