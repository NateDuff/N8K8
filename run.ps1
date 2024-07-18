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

# Steps roughly from here: https://github.com/devkimchi/aspir8-from-scratch
# docker login --------.azurecr.io -u acrn8k8 -p --------

# aspirate init -cr -----.azurecr.io -ct latest
# aspirate generate --image-pull-policy Always
# aspirate apply -k aks-n8k8 --non-interactive


# aspirate build

# aspirate start
# aspirate stop


