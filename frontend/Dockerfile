# Vite 8 pide Node >= 22.12 (o 20.19+). Alpine para que la etapa de build no engorde el caché.
FROM node:22-alpine AS build
WORKDIR /app

# package.json + lockfile primero: capa cacheable, npm ci solo se re-ejecuta si cambian dependencias.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# `npm run build` = vue-tsc -b && vite build — si hay error de tipos, el build falla acá y no
# se publica una imagen rota.
RUN npm run build

# nginx sirve el bundle estático y hace de reverse proxy de /api hacia el contenedor del API.
# Así el frontend y el API quedan en el mismo origen: sin CORS y sin exponer el API al exterior.
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD wget --quiet --spider http://127.0.0.1/ || exit 1
