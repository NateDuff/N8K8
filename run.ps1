## Delete everything: kubectl delete all --all

kubectl apply -f ./.k8s/rabbitmq-deployment.yaml
kubectl apply -f ./.k8s/rabbitmq-service.yaml

kubectl apply -f ./.k8s/web-deployment.yaml
kubectl apply -f ./.k8s/api-deployment.yaml

kubectl apply -f ./.k8s/web-service.yaml
kubectl apply -f ./.k8s/api-service.yaml

kubectl apply -f ./.k8s/worker-deployment.yaml

#kubectl apply -f ./.k8s/job-deployment.yaml
