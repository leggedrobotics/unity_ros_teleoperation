## CRS
Set up CRS according to their documentation

**Important:** We have to expose port 10000 and 5555 if we are running inside the docker container. This means we have to add a port mapping to the `docker-compose.yaml` file as below:

```yaml
    ports:
      - "20210-20230:20210-20230/udp"
      - "33300-33400:33300-33400/udp"
      - "10000:10000" <--- add this
      - "5005:5005"   <--- add this
```

## ROS
Once we are inside the docker container (with `crs-docker run`) add the [ROS TCP Enpoint package](https://github.com/leggedrobotics/ROS-TCP-Endpoint) (its a ros package) inside the crs docker.

Now start a simulation using
```bash
roslaunch crs_launch sim_single_car.launch
```
or 
```bash
roslaunch crs_launch sim_single_car.launch bypass_estimator:=false
```
for enabling the estimation node to publish

To open another terminal: 
```bash
docker ps 
docker exec -it fb099fdd9426 bash 

To start the tcp endpoint run
```bash
roslaunch ros_tcp_endpoint endpoint.launch
```





The messages are automatically published to the network if the ROS TCP Endpoint is running.

The `rostopic` command is very useful if you want to know what is going on in the system:
`rostopic list`: lists all topics that are active
`rostopic echo <topic>`: show the messages from a topic
`rostopic info <topic>`: show information (type, publishers, subscriber) from a topic
## Connecting with unity
In unity we use the [ROS-TCP-Endpoint package](https://github.com/leggedrobotics/ROS-TCP-Endpoint?tab=readme-ov-file). Follow the setup instructions from [here](https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/ros_unity_integration/setup.md). It involves generating the scripts to parse the messages etc. and setting up the correct IP address (all in Robotics menu in unity)
## Running on MetaQuest
Follow the Unity Quickstart and Quest Quickstart guides from [here](https://github.com/leggedrobotics/unity_ros_teleoperation).

**Important:** To be able to get a connection on the Quest 3, before building you have to always uncheck "Force Remove Internet" as shown in the Quest Quickstart guide.

