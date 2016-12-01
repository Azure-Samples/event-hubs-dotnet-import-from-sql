---
services: event-hubs, cloud-services, sql-database
platforms: dotnet
author: spyrossak
---

# SQL to Event Hub scenario #

This scenario is pretty rare, and you should be sure that it applies to you before continuing! 
Generally speaking, if you are using Event Hubs, you are working with streaming data, which you may end up 
storing in SQL. This project deals with the opposite, taking data out of SQL and pushing it to an Event Hub.
The reason this is rare is that the data stored in SQL is <i>usually</i> more static than dynamic, and it would make little
sense to stream it anywhere. On occassion, however, you may actually need to do this. The anticipated scenario
is one where a SQL table is being updated from some remote source, and you want those updates to be 
streamed so that they can be visualized or merged with other streaming data. The code in this project is an 
example of how to do this. It is not a supported solution, nor a recommended one, but simply an example to 
show you how it can be done.

For more information about this sample, see the [Pulling data from SQL into an Azure Event Hub](https://github.com/Azure-Samples/event-hubs-dotnet-import-from-sql/event-hubs-pulling-public-data.md) topic in this repository.

## Prerequisites ##

* An Azure subscription
* A version of Visual Studio installed on your desktop
* An Azure SQL table being updated with data as the source of data  
  * A field in that table that is a unique, growing value - for example the record number 
* A Service Bus namespace and an Event Hub as the target for the data

## Setup Tasks ##

Setting up the application once you have an Event Hub and its Connection String involves the following tasks, which will be described in greater detail below.

1. Clone or copy the project to your machine 
2. Open the project solution in Visual Studio
3. Edit App.config in the SqlToEventHub folder to provide the relevant source and target configuration data
4. Build the project
5. Publish the application to your Azure subscription
6. Verify that data is coming in to your Event Hub from the SQL table


## Editing App.config ##

All the parameters you need to specify to get this project to run are in the ```<appSettings>``` section of the 
App.config file. There are two sections, one for the SQL table you will poll for updates, and one for the event hub 
to which you will send those updates.

### The SQL table information ###

In the ```<appSettings>``` section, enter

* The **connection string to your SQL database**. Find the line in the ```<appSettings>``` section that says
```
<add key="sqlDatabaseConnectionString" value="[Sql connection string]"/>  
```
Replace ```[Sql connection string]``` with the connection string to the SQL database. In the [Azure portal](http://ms.portal.azure.com), 
 select SQL databases from the left nav menu, and then your SQL database in the right hand pane. In the next pane, click All settings. In the Settings pane, under GENERAL
 click Properties. Finally, in the Properties pane, under CONNECTION STRINGS click Show database connection strings. The Database
 connection strings pane will show four connection strings. Copy the first one, labeled ADO.NET.
It should look something like the following:  
```
Server=tcp:myserver.database.windows.net,1433;Database=mydatabase;User ID=myadmin@myserver;Password={your_password_here};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```
Replace ```[Sql connection string]``` with this whole string.

* The **name of the table that holds the data you want to pull**. Find the line in the ```<appSettings>``` section that says
```
<add key="DataTableName" value="[datatable]" />  
```
Replace ```[datatable]``` with the name of your table (without the ```dbo.``` prefix) 


* The **name of the field in your SQL table that is a unique record id**. Find the line in the ```<appSettings>``` section that says
```
<add key="OffsetKey" value="[row_id]" /> 
```
Replace ```[row_id]``` with the name of that field in your table. NOTE THIS IS CASE SENSITIVE. Finally, 

* The **interval at which your cloud service should poll the SQL table**. Find the line in the ```<appSettings>``` section that says
```
<add key="SleepTimeMs" value="10000"/> 
```
Replace ```10000``` with the interval that you want to use, in milliseconds. The default is 10 seconds. 



### The Event Hub information ###

Also in the ```<appSettings>``` section, enter


* The **name of the Event Hub** to which you want the data sent. Find the line in the appSettings section that says
```
<add key="Microsoft.ServiceBus.EventHubToUse" value="[event hub name]" />  
```
Replace ```[event hub name]``` with the name of the event hub that you created.

* The **connection string to your Service Bus**. To find it, in the [Azure classic portal](http://manage.windowsazure.com),
 select Service Bus from the left nav menu, highlight your Namespace Name in the right pane, and click on 
 Connection Information at the bottom of the page. In the Access connection information window that opens, 
 highlight and copy the Connection String shown. In the new [Azure portal](http://ms.portal.azure.com), 
 select Browse from the left nav menu, pick Service bus namespaces from the list, and then your Service 
 Bus Namespace from the right pane to get to a screen where you can get the Connection Screen 
 information.  It should look something like the following:   
```
Endpoint=sb://myservicebusname.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Axf5bbXYZeEaLoIeLMN2RV1sc3HdkYxFq7RX/T6a5TE=
```
Find the ```<appSettings>``` section in App.Config, and the line that says
```
<add key="Microsoft.ServiceBus.ServiceBusConnectionString" value="[Service Bus connection string]" />
```
Replace ```[Service Bus connection string]``` with the whole of the string that you copied from the 
portal, starting with "Endpoint" and ending with an "=".


## Publishing the application ##

1. In Visual Studio, right-click on 'WorkerRole' in Solution 'SqlToEventHub', and select *Publish*.
2. In the Publish Azure Application, answer the following questions. 
    * Name: [pick something unique]
    * Region: [pick same region as you used for the Event Hub]
    * Database server: no database
    * Database password: [leave suggested password]
3. Click Publish, and wait until the status bar shows "Completed". At that point the application is running in your subscription, 
polling the SQL table for any records that have been added since the last time it was polled, and pushing those records to your Event Hub. 
From there, you can access with Stream Analytics or any other application as you would normally.

