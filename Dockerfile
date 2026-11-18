FROM ubuntu:24.04 AS build
ENV DEBIAN_FRONTEND=noninteractive \
    VCPKG_ROOT=/opt/vcpkg \
    VCPKG_DISABLE_METRICS=1
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential ca-certificates cmake curl git ninja-build pkg-config python3 unzip zip tar \
    libssl-dev && rm -rf /var/lib/apt/lists/*
RUN git clone https://github.com/microsoft/vcpkg.git "$VCPKG_ROOT" \
    && "$VCPKG_ROOT/bootstrap-vcpkg.sh" -disableMetrics
WORKDIR /src
COPY . .
RUN cmake --preset release -DBUILD_TESTING=OFF
RUN cmake --build --preset release \
    && strip build/release/roster-agent

FROM ubuntu:24.04 AS runtime
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates libcurl4 libsqlite3-0 libssl3 \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --system --uid 10001 --create-home roster \
    && mkdir -p /data /app \
    && chown roster:roster /data /app
WORKDIR /app
COPY --from=build --chown=roster:roster /src/build/release/roster-agent /app/roster-agent
COPY --chown=roster:roster config.toml /app/config.toml
USER roster
ENTRYPOINT ["/app/roster-agent", "--config", "/app/config.toml"]
