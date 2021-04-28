# MQTT AMQP gRPC Sample

For comparison each protocols.  
MassTransit 7.1.8  
https://masstransit-project.com

## Make certification files
Make locally-trusted development certificates and put them into key folder.  
https://github.com/FiloSottile/mkcert
```
$ mkcert -install
$ mkcert broker.local bff.local edgenode.local localhost 127.0.0.1 ::1
```

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
