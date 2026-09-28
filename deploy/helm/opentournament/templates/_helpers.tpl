{{- define "opentournament.name" -}}
{{- .Chart.Name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "opentournament.fullname" -}}
{{- if contains .Chart.Name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name .Chart.Name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}

{{- define "opentournament.selectorLabels" -}}
app.kubernetes.io/name: {{ include "opentournament.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "opentournament.labels" -}}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{ include "opentournament.selectorLabels" . }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "opentournament.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "opentournament.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{- define "opentournament.image" -}}
{{- printf "%s:%s" .Values.image.repository (default .Chart.AppVersion .Values.image.tag) }}
{{- end }}

{{/* The namespaces of every Fleet in the catalog, as the service computes them. */}}
{{- define "opentournament.fleetNamespaces" -}}
{{- $namespaces := list }}
{{- range .Values.catalog.games }}
{{- $namespaces = append $namespaces (default "default" (dig "fleet" "namespace" "" .)) }}
{{- end }}
{{- $namespaces | uniq | toJson }}
{{- end }}

{{/* Fails the render when a required value is missing. */}}
{{- define "opentournament.validate" -}}
{{- if not .Values.config.oidcIssuer }}
{{- fail "config.oidcIssuer is required" }}
{{- end }}
{{- if not (or .Values.database.url .Values.database.existingSecret) }}
{{- fail "set database.url or database.existingSecret" }}
{{- end }}
{{- if not .Values.catalog.games }}
{{- fail "catalog.games needs at least one Game" }}
{{- end }}
{{- end }}

{{- define "opentournament.databaseSecretName" -}}
{{- default (include "opentournament.fullname" .) .Values.database.existingSecret }}
{{- end }}

{{- define "opentournament.databaseSecretKey" -}}
{{- if .Values.database.existingSecret }}{{ .Values.database.existingSecretKey }}{{ else }}OT_DATABASE_URL{{ end }}
{{- end }}

{{- define "opentournament.matchTokenSecretName" -}}
{{- default (include "opentournament.fullname" .) .Values.matchTokenKey.existingSecret }}
{{- end }}

{{- define "opentournament.matchTokenSecretKey" -}}
{{- if .Values.matchTokenKey.existingSecret }}{{ .Values.matchTokenKey.existingSecretKey }}{{ else }}OT_MATCH_TOKEN_KEY{{ end }}
{{- end }}
