#!/bin/bash
# deploy-plugin.sh

# Configuratie - pas deze aan voor jouw setup
NAMESPACE="jellyfin"  # Vervang met jouw namespace
POD_SELECTOR=""      # We bepalen dit dynamisch

echo "Looking for Jellyfin pods..."

# Probeer verschillende manieren om de pod te vinden
if kubectl get pods -n $NAMESPACE -l app=jellyfin &>/dev/null; then
    POD_NAME=$(kubectl get pods -n $NAMESPACE -l app=jellyfin -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
elif kubectl get pods -n $NAMESPACE -l app.kubernetes.io/name=jellyfin &>/dev/null; then
    POD_NAME=$(kubectl get pods -n $NAMESPACE -l app.kubernetes.io/name=jellyfin -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
else
    # Handmatig zoeken naar pods met "jellyfin" in de naam
    POD_NAME=$(kubectl get pods -n $NAMESPACE -o name | grep jellyfin | head -1 | cut -d'/' -f2)
fi

# Als nog geen pod gevonden, laat gebruiker kiezen
if [ -z "$POD_NAME" ]; then
    echo "Could not automatically find Jellyfin pod. Available pods in namespace '$NAMESPACE':"
    kubectl get pods -n $NAMESPACE
    echo ""
    echo "Please enter the Jellyfin pod name:"
    read POD_NAME
fi

if [ -z "$POD_NAME" ]; then
    echo "Error: No pod name provided"
    exit 1
fi

echo "Using pod: $POD_NAME in namespace: $NAMESPACE"

PLUGIN_DIR="/config/plugins/Jellyfin.Plugin.MinioBackup"

echo "Deploying plugin to pod: $POD_NAME"

# Test of pod bestaat
if ! kubectl get pod $POD_NAME -n $NAMESPACE &>/dev/null; then
    echo "Error: Pod $POD_NAME not found in namespace $NAMESPACE"
    exit 1
fi

# Create plugin directory
echo "Creating plugin directory..."
kubectl exec $POD_NAME -n $NAMESPACE -- mkdir -p $PLUGIN_DIR

# Check if build files exist
if [ ! -f "bin/Release/net8.0/Jellyfin.Plugin.MinioBackup.dll" ]; then
    echo "Error: Plugin DLL not found. Did you run 'dotnet build --configuration Release'?"
    exit 1
fi

# Copy plugin files to the plugin folder
echo "Copying plugin files..."
kubectl cp bin/Release/net8.0/Jellyfin.Plugin.MinioBackup.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/
kubectl cp bin/Release/net8.0/manifest.json $NAMESPACE/$POD_NAME:$PLUGIN_DIR/

# Copy dependencies
echo "Copying dependencies..."
kubectl cp bin/Release/net8.0/Minio.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/
kubectl cp bin/Release/net8.0/CommunityToolkit.HighPerformance.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/

# Check if SQLite DLL exists (might be in different location)
#if [ -f "bin/Release/net8.0/Microsoft.Data.Sqlite.dll" ]; then
#    kubectl cp bin/Release/net8.0/Microsoft.Data.Sqlite.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/
#else
#    echo "Warning: Microsoft.Data.Sqlite.dll not found in expected location"
#fi

# Verify installation
echo "Verifying installation..."
kubectl exec $POD_NAME -n $NAMESPACE -- ls -la $PLUGIN_DIR/

echo "Plugin deployed to $PLUGIN_DIR! Restarting Jellyfin..."
kubectl delete pod $POD_NAME -n $NAMESPACE

echo "Done! Plugin will be available after pod restart."
echo "Check plugin status with: kubectl logs -f <new-pod-name> -n $NAMESPACE"
