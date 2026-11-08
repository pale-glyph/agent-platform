FROM node:22-alpine AS build

WORKDIR /app
COPY apps/agent-platform-admin/package.json ./
RUN npm install

COPY apps/agent-platform-admin/ ./
RUN npm run build && npm prune --omit=dev

FROM node:22-alpine

ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3000

WORKDIR /app
COPY --from=build /app ./

EXPOSE 3000
CMD ["npm", "run", "start"]
