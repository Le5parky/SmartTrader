#!/bin/sh
set -euo pipefail

MIGRATION_BIN="./migrate"

if [ -f "$MIGRATION_BIN" ]; then
    echo "Applying EF Core migrations..."
    CONNECTION="${ConnectionStrings__CoreDatabase:-}"
    if [ -z "$CONNECTION" ]; then
        CONNECTION="${ConnectionStrings__MarketDatabase:-}"
    fi

    if [ -n "$CONNECTION" ]; then
        "$MIGRATION_BIN" --connection "$CONNECTION"
    else
        echo "Skipping migrations: connection string not provided" >&2
    fi
else
    echo "Migration bundle not found at $MIGRATION_BIN; skipping" >&2
fi

exec dotnet SmartTrader.Presentation.AdminApi.dll "$@"
