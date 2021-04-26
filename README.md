# MQTT & AMQP & gRPC Sample

For comparison protocols.  
MassTransit 7.1.8  
https://masstransit-project.com

## Build Container Images

### Bff
```
docker build -t bff -f Bff/Dockerfile .
```

### Front
```
docker build -t front -f Front/Dockerfile .
```

### EdgeNode
```
docker build -t edgenode -f EdgeNode/Dockerfile .
```

### Broker
```
docker build -t broker -f Broker/Dockerfile .
```

## Launch
```
docker-compose up -d
```
