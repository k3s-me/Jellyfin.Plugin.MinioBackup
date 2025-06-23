#!/bin/bash
# deploy-plugin.sh

# Configuration - adjust for your setup
NAMESPACE="jellyfin"  # Replace with your namespace
POD_SELECTOR=""      # We determine this dynamically

echo "Looking for Jellyfin pods..."

# Try different ways to find the pod
if kubectl get pods -n $NAMESPACE -l app=jellyfin &>/dev/null; then
    POD_NAME=$(kubectl get pods -n $NAMESPACE -l app=jellyfin -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
elif kubectl get pods -n $NAMESPACE -l app.kubernetes.io/name=jellyfin &>/dev/null; then
    POD_NAME=$(kubectl get pods -n $NAMESPACE -l app.kubernetes.io/name=jellyfin -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
else
    # Manually search for pods with "jellyfin" in the name
    POD_NAME=$(kubectl get pods -n $NAMESPACE -o name | grep jellyfin | head -1 | cut -d'/' -f2)
fi

# If no pod found yet, let user choose
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

# Test if pod exists
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

# Copy main plugin files
echo "Copying plugin files..."
kubectl cp bin/Release/net8.0/Jellyfin.Plugin.MinioBackup.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/
kubectl cp bin/Release/net8.0/manifest.json $NAMESPACE/$POD_NAME:$PLUGIN_DIR/

# Copy required dependencies
echo "Copying dependencies..."
kubectl cp bin/Release/net8.0/Minio.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/
kubectl cp bin/Release/net8.0/CommunityToolkit.HighPerformance.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/

# Copy optional dependencies if they exist
if [ -f "bin/Release/net8.0/System.Reactive.dll" ]; then
    echo "Copying System.Reactive.dll..."
    kubectl cp bin/Release/net8.0/System.Reactive.dll $NAMESPACE/$POD_NAME:$PLUGIN_DIR/
fi

# Verify installation
echo "Verifying installation..."
kubectl exec $POD_NAME -n $NAMESPACE -- ls -la $PLUGIN_DIR/

echo "Plugin deployed to $PLUGIN_DIR successfully!"
echo ""
echo "Restarting Jellyfin pod to load the plugin..."
kubectl delete pod $POD_NAME -n $NAMESPACE

echo ""
echo "✅ Deployment complete!"
echo "📝 The plugin will be available after pod restart."
echo "🔍 Check plugin status with:"
echo "   kubectl logs -f <new-pod-name> -n $NAMESPACE"
echo ""
echo "🎯 Access plugin configuration via:"
echo "   Jellyfin Dashboard → Plugins → MinIO Backup"