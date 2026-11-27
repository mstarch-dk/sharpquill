# Build stage
FROM node:26-alpine AS builder

WORKDIR /app

# Copy package files
COPY web/package*.json ./

# Install dependencies
RUN npm ci

# Copy the shared themes and the shared Nuxt layer (nuxt.config.ts: extends ../layers/shared)
COPY themes/ ../themes/
COPY layers/ ../layers/

# Copy source code
COPY web/ .

# Build the application
RUN npm run build

# Production stage
FROM node:26-alpine AS runner

WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nuxtjs

# Copy built application
COPY --from=builder /app/.output ./.output

# Set ownership
RUN chown -R nuxtjs:nodejs /app

USER nuxtjs

# Expose port
EXPOSE 3000

# Set environment variables
ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -q --spider http://127.0.0.1:3000/ || exit 1

# Start the application
CMD ["node", ".output/server/index.mjs"]
