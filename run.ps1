## Delete everything: kubectl delete all --all

# kubectl apply -f ./.k8s/rabbitmq-deployment.yaml
# kubectl apply -f ./.k8s/rabbitmq-service.yaml

kubectl apply -f ./.k8s/web-deployment.yaml
kubectl apply -f ./.k8s/api-deployment.yaml

kubectl apply -f ./.k8s/web-service.yaml
kubectl apply -f ./.k8s/api-service.yaml

kubectl apply -f ./.k8s/worker-deployment.yaml

#kubectl apply -f ./.k8s/job-dotnet-deployment.yaml
#kubectl apply -f ./.k8s/job-pwsh-deployment.yaml

# azd up

# aspirate generate --output-format helm
# aspirate build

# aspirate start
# aspirate stop

# helm upgrade demo .\aspirate-output\Chart\ --install --namespace aspire-demo --create-namespace --wait
# helm uninstall demo --namespace aspire-demo
