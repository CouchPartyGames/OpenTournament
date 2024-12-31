FROM node:20.10-bookworm-slim as build

ENV NODE_ENV=production 

WORKDIR /app

COPY package.json ./
COPY package-lock.json ./

# pnpm must be installed as it doesn't come with the default image
RUN npm i
COPY . ./

RUN npm build

FROM node:20.10-bookworm-slim

WORKDIR /app
COPY --from=build /app .
# ENV HOST is not though you can uncomment if if needed
# ENV HOST=0.0.0.0
EXPOSE 4173

CMD ["node","build"]