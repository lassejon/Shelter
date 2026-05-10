# Postgres 18 with PostGIS 3 baked in via the PGDG apt repository (already configured
# inside the official postgres image). Using the official base instead of postgis/postgis
# keeps the supply chain to a single trusted upstream and gives us native arm64 support.
FROM postgres:18

RUN apt-get update \
 && apt-get install -y --no-install-recommends \
        postgresql-18-postgis-3 \
        postgresql-18-postgis-3-scripts \
 && rm -rf /var/lib/apt/lists/*
