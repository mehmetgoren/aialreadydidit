# ---- build stage -----------------------------------------------------------
FROM node:22-alpine AS build-stage
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# API root baked at build time; override with --build-arg VITE_API_BASE_URL=/api/v1 to use the nginx proxy below
ARG VITE_API_BASE_URL=/api/v1
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build-only

# ---- runtime stage ---------------------------------------------------------
FROM nginx:1.27-alpine AS production-stage
ENV TZ=Europe/Istanbul
RUN apk add --no-cache tzdata
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/security-headers.conf /etc/nginx/security-headers.conf
COPY --from=build-stage /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
