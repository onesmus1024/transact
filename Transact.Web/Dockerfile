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

# Reverse-proxy + static-file config (also fronts the API at /api).
COPY nginx/default.conf /etc/nginx/conf.d/default.conf

# Angular build output is written to dist/<project>/browser in v17+.
COPY --from=build /src/dist/Transact.Web/browser /usr/share/nginx/html

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://127.0.0.1/ >/dev/null 2>&1 || exit 1
