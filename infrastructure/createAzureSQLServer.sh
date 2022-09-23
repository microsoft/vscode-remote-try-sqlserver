# Create a single database and configure a firewall rule
# Variable block
let "randomIdentifier=$RANDOM*$RANDOM"
location="East US"
resourceGroup="rg-trycontainer-azuresql-$randomIdentifier"
server="trycontainer-azuresql-server-$randomIdentifier"
database="trycontazuresqldb$randomIdentifier"
login="azureuser"
password="Pa$$w0rD-$randomIdentifier"
# Specify appropriate IP address values for your environment
# to limit access to the SQL Database server
startIp=0.0.0.0
endIp=0.0.0.0

echo "Using resource group $resourceGroup with login: $login, password: $password..."
echo "Creating $resourceGroup in $location..."
az group create --name $resourceGroup --location "$location"
echo "Creating $server in $location..."
az sql server create --name $server --resource-group $resourceGroup --location "$location" --admin-user $login --admin-password $password
echo "Configuring firewall..."
az sql server firewall-rule create --resource-group $resourceGroup --server $server -n AllowYourIp --start-ip-address $startIp --end-ip-address $endIp
echo "Creating $database on $server..."
az sql db create --resource-group $resourceGroup --server $server --name $database --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless --auto-pause-delay 60
connectionString=$(az sql db show-connection-string -s $server -n $database -c ado.net)
connectionString=${connectionString//<username>/$login}
connectionString=${connectionString//<password>/$password}
echo "Database created on $resourceGroup. ConnectionString:"
echo "$connectionString"
echo "To clean-up resources, type az group delete --name $resourceGroup"