kubectl apply -f ./.k8s/web-deployment.yaml
kubectl apply -f ./.k8s/api-deployment.yaml

kubectl apply -f ./.k8s/web-service.yaml
kubectl apply -f ./.k8s/api-service.yaml
