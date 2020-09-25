using Microsoft.Azure.Management.PostgreSQL.FlexibleServers;
using Microsoft.Azure.Management.PostgreSQL.FlexibleServers.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Fluent;
using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace PostgreSQLFlexibleServersSample
{
    class Sample
    {

        private static readonly string AdministratorLogin = "pgsqladmin3423";
        private static readonly string AdministratorPassword = "myS3cureP@ssword";

        public static void RunSample(IAzure azure, AzureCredentials creds)
        {
            string serverName = SdkContext.RandomResourceName("netserver", 20);
            string rgName = SdkContext.RandomResourceName("rgPorstgreSql", 20);
            
            try
            {

                // Create the resource group.
                var resourceGroup = azure.ResourceGroups
                        .Define(rgName)
                        .WithRegion(Region.USEast2)
                        .Create();

                Console.WriteLine("Resource group with name " + rgName + " is created.");

                // Create the pg client and authenticate using the creds.
                PostgreSQLManagementClient client = new PostgreSQLManagementClient(creds);
                client.SubscriptionId = azure.SubscriptionId;

                // Create the server.
                Server server = client.Servers.Create(
                    rgName,
                    serverName,
                    new Server(location : "eastus2",
                               sku : new Sku("Standard_D4s_v3", "GeneralPurpose"),
                               administratorLogin: AdministratorLogin,
                               administratorLoginPassword: AdministratorPassword,
                               version: "12",
                               storageProfile: new StorageProfile(storageMB: 524288)
                ));

                Console.WriteLine("Server with name " + serverName + " is created");

                // Update the StorageMB.
                server = client.Servers.Update(
                    rgName,
                    serverName,
                    new ServerForUpdate(storageProfile : new StorageProfile(storageMB: 1048576))
                    );
                
                Console.WriteLine("Server's storageMB has been updated");

                // Create a firewall rule.
                FirewallRule fwRule = client.FirewallRules.CreateOrUpdate(
                    rgName,
                    serverName,
                    "FirewallRule",
                    new FirewallRule(
                        startIpAddress : "10.0.0.1",
                        endIpAddress : "10.0.0.1"
                    )
                );

                Console.WriteLine("Created a firewall rule.");

                // Update a firewall rule.
                fwRule = client.FirewallRules.CreateOrUpdate(
                    rgName,
                    serverName,
                    "FirewallRule",
                    new FirewallRule(
                        startIpAddress: "10.0.0.255",
                        endIpAddress : "10.0.0.255"
                    )
                );

                Console.WriteLine("Updated a firewall rule.");

                var rules = client.FirewallRules.ListByServer(
                    rgName,
                    serverName
                );

                Console.WriteLine("List firewall rules:");

                foreach(FirewallRule rule in rules) {
                    Console.WriteLine(rule.StartIpAddress + " " + rule.EndIpAddress);
                }

                Console.WriteLine("Delete the firewall rule");
                
                client.FirewallRules.Delete(
                    rgName,
                    serverName,
                    "FirewallRule"
                );

                // Get a configuration.
                Configuration configuration = client.Configurations.Get(
                    rgName,
                    serverName,
                    "max_replication_slots"
                );

                Console.WriteLine("Got configuration max_replication_slots with value " + configuration.Value);

                // Update the configuration.
                configuration.Value = "20";
                configuration.Source = "user-override";

                configuration = client.Configurations.Update(
                    rgName,
                    serverName,
                    "max_replication_slots",
                    configuration
                );
                
                Console.WriteLine("Updated configuration max_replication_slots new value ", configuration);

                // Delete the server.
                client.Servers.Delete(
                    rgName,
                    serverName
                );
                
                Console.WriteLine("Server is deleted");

                // Delete the resource group.
                azure.ResourceGroups.DeleteByName(rgName);

                Console.WriteLine("Resource group with name " + rgName + " is deleted");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                // Clean the resource group.
                azure.ResourceGroups.DeleteByName(rgName);
            }

        }
        
        static void Main(string[] args)
        {
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var secret = Environment.GetEnvironmentVariable("AZURE_SECRET");
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            // Authenticate with Service Principal.
            AzureCredentials creds = new AzureCredentialsFactory().FromServicePrincipal(clientId, secret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Authenticate(creds).WithSubscription(subscriptionId);

            RunSample(azure, creds);

            return;
        }
    }
}