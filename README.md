# NServiceBus.FileBasedRouting
A simple implementation of a static, file based routing logic to use with MSMQ. It replaces all routing configurations and the subscription storage with XML files to configure message destinations and sender-side distribution easily across all endpoints.


## Configuration


Enable the `FileBasedRouting` feature in the endpoint configuration:

    endpointConfiguration.EnableFeature<FileBasedRoutingFeature>();
    
Configure file based routing as your subscription storage for events and disabled `AutoSubscribe` (as its no longer used):

    endpointConfiguration.UsePersistence<StaticRoutingPersistence, StorageType.Subscriptions>();
    endpointConfiguration.DisableFeature<AutoSubscribe>();
    
Create a new XML file named `endpoints.xml` and include it on every endpoint using file based routing. Make sure that the file is copied to the binaries.
Sample configuration file:

```
<endpoints>
  <endpoint name="endpointA">
    <handles>
      <event type="Contracts.Events.DemoCommandReceived, Contracts" />
    </handles>
  </endpoint>
  <endpoint name="endpointB">
    <handles>
      <command type="Contracts.Commands.DemoCommand, Contracts" />
      <event type="Contracts.Events.DemoEvent, Contracts" />
    </handles>
  </endpoint>
</endpoints>
```

* For every logical endpoint, add an `endpoint` element with a `name` attribute to match the configured endpoint name in the `EndpointConfiguration`.
* For every command handled by this endpoint, add an `command` element to the `handles` collection with a `type` attribute containing the **assembly qualified name** of the message to handle.
* For every event received by this endpoint, add an `event` element to the `handles` collection with a `type` attribute containing the  **assembly qualified name** of the event to handle.

That's it.


### Updating the routing configuration

The routing configuration is read every 30 seconds. You can therefore change the routing at runtime (e.g. unsubscribe an endpoint by removing its `event` entry from the `handles` collection).


## Scaling out

It's possible to use sender-side distribution to scale out messages and events to multiple instances of the same logical endpoint. This is done with the instance mapping file documented here:https://docs.particular.net/nservicebus/msmq/routing?version=Core_6

In short: create a new xml file named `instance-mapping.xml` and include it on every endpoint. Make sure to copy the file over to the binaries folder.

```
<endpoints>
  <endpoint name="endpointB">
    <instance discriminator="1" machine="machine1" />
    <instance discriminator="2" machine="machine1" />
    <instance machine="machine2" />
  </endpoint>
</endpoints>
```

* For every logical endpoint with scaled out instances, add an `endpoint` element with the matching endpoint name.
* Add an `instance` element for every available instance
  * Configure the `discriminator` if a discriminator has been configured
  * Configure the machine of the instance using the `machine` attribute
