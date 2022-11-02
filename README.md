# Try Out Development Containers: SQL Server & Azure SQL

[![Open in Remote - Containers](https://img.shields.io/static/v1?label=Remote%20-%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/microsoft/vscode-remote-try-sqlserver)

A **development container** is a running [Docker](https://www.docker.com) container with a well-defined tool/runtime stack and its prerequisites. You can try out development containers with **[GitHub Codespaces](https://github.com/features/codespaces)** or **[Visual Studio Code Remote - Containers](https://aka.ms/vscode-remote/containers)**.

This is a sample project that lets you try out either option in a few easy steps. We have a variety of other [vscode-remote-try-*](https://github.com/search?q=org%3Amicrosoft+vscode-remote-try-&type=Repositories) sample projects, too.

> **Note:** If you already have a Codespace or dev container, you can jump to the [Things to try](#things-to-try) section.


## Setting up the development container

### GitHub Codespaces

Follow these steps to open this sample in a Codespaces:

1. Click the Code drop-down menu and select the **Codespaces** tab.
1. Click on **Create codespaces on main** at the bottom of the pane.

For more info, check out the [GitHub documentation](https://docs.github.com/en/free-pro-team@latest/github/developing-online-with-codespaces/creating-a-codespace#creating-a-codespace).

### VS Code Dev Containers

If you already have VS Code and Docker installed, you can click the badge above or [here](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/microsoft/vscode-remote-try-sqlserver) to get started. Clicking these links will cause VS Code to automatically install the Dev Containers extension if needed, clone the source code into a container volume, and spin up a dev container for use.

Follow these steps to open this sample in a container using the VS Code Dev Containers extension:

1. If this is your first time using a development container, please ensure your system meets the pre-reqs (i.e. have Docker installed) in the [getting started steps](https://aka.ms/vscode-remote/containers/getting-started).

2. To use this repository, you can either open the repository in an isolated Docker volume:

    - Press <kbd>F1</kbd> and select the **Dev Containers: Try a Sample...** command.
    - Choose the ".NET Core" sample, wait for the container to start, and try things out!
        > **Note:** Under the hood, this will use the **Dev Containers: Clone Repository in Container Volume...** command to clone the source code in a Docker volume instead of the local filesystem. [Volumes](https://docs.docker.com/storage/volumes/) are the preferred mechanism for persisting container data.

   Or open a locally cloned copy of the code:

   - Clone this repository to your local filesystem.
   - Press <kbd>F1</kbd> and select the **Dev Containers: Open Folder in Container...** command.
   - Select the cloned copy of this folder, wait for the container to start, and try things out!

## Things to try

Once you have this sample opened, you'll be able to work with it like you would locally.

> **Note:** This container runs as a non-root user with sudo access by default. Comment out `"remoteUser": "vscode"` in `.devcontainer/devcontainer.json` if you'd prefer to run as root.

1. **Connect via SQLCMD and create a new database**

    SQLCMD is already installed within the container. You can use it from the **Terminal** tab, using the *bash* shell. For example, you can execute a SQL Script. This example creates a new database.

    ```sql
    sqlcmd -S localhost -U sa -P P@ssw0rd -d master -i 01-CreateDatabase.sql
    ```

    > Note: The SQL Server instance is created with user `sa` and password `P@ssw0rd`. You will need it for the next steps. This password is defined in the `devcontainer.json` file. This password is not secure. It could be used in local development scenarios but must not be used elsewhere (hosted team development server, or production server).

2. **Deploy schema with SQL Database projects**

    SQL Server Database projects allow you to organize the code artifacts, generate a dacpac, or easily deploy schema changes on an instance. In this repository, you'll find a sample Database project that creates a single `User` table and populate it with some records. Let's deploy it on the SQL Instance integrated into the dev container.

    - On the primary sidebar (on the left), click on **Database projects** tab.
    - The _Database Projects_ pane appears. The `TryDbProjects` project shows up. You can just right-click the database project name and click **Publish**. You'll have a series of prompts. Answer with these items (Prompt -> Answer):
        - Select where to publish the project to -> Publish to an existing SQL server
        - Select publish profile to load -> Don't use profile
        - Choose a connection profile from the list below -> mssql-container
        - Select database -> TryDbProject
        - Choose action -> Publish
    - After a minute or so, the database schema should be deployed. You can follow the deployment via the notification or through _Database Projects_ output. 

> Note: you might have to update the extension setting _dotnet SDK location_ to `/usr/bin/` to execute the publish step. This setting is called _Dotnet SQL Location_ under _Extensions_ > _Database Projects_.

3. **Explore your database with SQL Server extension**

    SQL Server extension allows you to explore your SQL Server and Azure SQL instances right within Visual Studio Code. Let's explore the database we have just created.

    - On the primary sidebar (on the left), click on **SQL Server** tab.
    - The _SQL Server_ pane appears. You should already see `mssql-container` listed. Click on it
    - Type the password listed previously
    - The treeview will be populated with Database items. For example, you can explore the tables or even select the rows in the `dbo.Users` table created with the SQL Database project at the previous step.

## Things to try with an Azure Subscription

This dev container uses the Azure CLI [dev container feature](https://github.com/devcontainers/features), that allows to interact with your Azure subscription. Here we will create a new Azure SQL Server and deploy our schema onto it. 

> Note: In this example, we use the Azure CLI to create our Azure resources. In a production environment, we would probably rely on Infrastructure tools like [Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview?tabs=bicep). 

1. **Login to your Azure account and create resources**

    - Open the terminal pane and type `az login --use-device-code`. Follow the instructions
    - Once logged-in, execute `./infrastructure/createAzureSQLServer.sh`
    - Copy the connection string, this will be necessary in the next step.

    This script will create an Azure SQL logical server, an Azure SQL Database, and will create a firewall rule opening the traffic to the whole Internet. **You should not use this script for production scenarios**. 
    
    >Remember to execute the Cleanup step below to avoid excessive billing.

2. **Prepare database project and deploy it to Azure**

    - On the primary sidebar (on the left), click on **Database projects** tab.
    - The _Database Projects_ pane appears. The `TryDbProjects` project shows up.
    - Right click on the project name, and click on **Change target platform**, and select **Azure SQL Database**
    - You can now deploy the database schema like you've done in the previous example, by using the connection string generated in step 1.

    Your database schema will be published. You can go back on the **SQL Server** Pane to explore it.

3. Cleanup Azure resources

    The scripts executed in step 1 creates Azure resources on your subscription. This will incur some billing. Once you've finished trying this feature, you can simply delete the created resource group. The Azure CLI command to execute is displayed on step 1. It looks like `az group delete --name resourceGroup`

> **About Azure Billing**
> In this example, we are using the [Serverless compute tier](https://learn.microsoft.com/en-us/azure/azure-sql/database/serverless-tier-overview?view=azuresql) of Azure SQL. This should incur a maximum billing of few euros if used for an hour. Depending on your subscription, you can leverage the [Azure Free Tier](https://azure.microsoft.com/free/).
> 
> As a good practice, in cloud environments, you should delete resources you don't use.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## License

Copyright © Microsoft Corporation All rights reserved.<br />
Licensed under the MIT License. See LICENSE in the project root for license information.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
