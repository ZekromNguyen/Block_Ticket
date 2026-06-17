# Legacy Monitoring Manifests

The old standalone Prometheus and Grafana manifests were removed during Phase 3 because they used static scrape configuration and default admin-style credentials.

Use:

- `k8s/gitops/argocd/observability-applications.yaml`
- `k8s/addons/observability`

Those resources install kube-prometheus-stack, Loki, Tempo, OpenTelemetry Collector, ServiceMonitors, PrometheusRules, Grafana datasources, and dashboards through ArgoCD.
