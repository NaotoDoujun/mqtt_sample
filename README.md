# MQTT AMQP gRPC Sample

For comparison each protocols.  
MassTransit 7.1.8  
https://masstransit-project.com  
MQTTnet  
https://github.com/chkr1011/MQTTnet  

## Make certification files
Make locally-trusted development certificates and put them into key folder.  
https://github.com/FiloSottile/mkcert
```
$ mkcert -install
$ mkcert broker.local bff.local edgenode.local camnode.local localhost 127.0.0.1 ::1
```
Created files should rename like this.
```
$ mv broker.local+6-key.pem server.key
$ mv broker.local+6.pem server.crt
```
Wheres my rootCA?
```
$ mkcert -CAROOT
```

## Build Container Images

### Bff
```
docker build -t bff -f Bff/Dockerfile .
```

### Front
note: material-ui/4.11.5 has tablepagination issue.  
```
docker build -t front -f Front/Dockerfile .
```

### EdgeNode
```
docker build -t edgenode -f EdgeNode/Dockerfile .
```

### CamNode (using qemu based Raspberry Pi OS)
```
docker build -t camnode -f CamNode/Dockerfile .
```

### Broker
```
docker build -t broker -f Broker/Dockerfile .
```

## Launch
```
docker-compose up -d
```

## ServiceType and TimeSpan Settings
Check out EdgeNode Dockerfile
```
# you can set serviceType(GRPC, AMQP, MQTT) and timeSpan(ms)
ENTRYPOINT ["dotnet", "EdgeNode.dll", "-s", "MQTT", "-t", "1000"]
```
