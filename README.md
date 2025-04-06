# Orlando Code Camp 2025 Session - Supercharge Your Microservices: ***Elevating DevExp and DevSecOps with Aspire and Dapr***
## Session Description
Microservices offer tremendous benefits, such as scalability, flexibility, and resilience, but they come with a tradeoff: the complexity of working with code in tightly coupled N-tier applications is replaced by the complexity of managing many loosely coupled services. Developer environment configuration and deployment become significant challenges, slowing value delivery and leading managers and teams to question whether the microservice promise is worth the pain.

In this session, we’ll explore how .NET Aspire and Dapr.io can simplify the complexities of microservice architecture, making it a realistic and achievable goal for teams of any size. We’ll demonstrate how these technologies streamline creation of development environments, reduce deployment headaches, and improve both developer experience (DevExp) and DevSecOps practices. Whether you're new to microservices or looking to optimize your existing approach, this session will provide actionable insights and strategies to help your team succeed.

## Install and Setup

### Prerequisites
- Docker
- Visual Studio Code
- Visual Studio 2022
- Aspire Workload
- NodeJS & NPM
- Dapr CLI

## Notes
- Frontend is not part of Visual Studio Solution but can be opened with VSCode
- `./AspireDaprDemo.AppHost.Program.cs` contains the orchestration configuration for local and cloud deployments
- `./AspireDaprDemo.AppHost/dapr` contains the dapr component configurations for local
- `./AspireDaprDemo.AppHost/infra` contains the bicep generated from the Program.cs
- `./.azdo/pipelines/azure-dev.yml` is the location of the pipeline generated during the talk

***To generate new bicep:***
```powershell
azd config set alpha.infraSynth on
azd infra synth -e "AspireDaprDemoDev" --debug --force
```

***To lint the generated bicep:***
```powershell
cd .\infra
az bicep build --file main.bicep
```

***To generate the pipeline:***
```powershell
azd auth login --tenant-id your_azdo_tenant_id.onmicrosoft.com
azd pipeline config --provider azdo
```

***To learn more:***
- [Dapr.io](https://www.dapr.io)
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
