FROM node:22-alpine AS build

WORKDIR /app
ARG JIM_AGENT_ID
ARG NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY
ENV NEXT_PUBLIC_JIM_AGENT_ID=$JIM_AGENT_ID
ENV NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY=$NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY
COPY apps/agent-platform-admin/package.json apps/agent-platform-admin/package-lock.json ./
RUN npm ci

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
