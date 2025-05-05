[back](/README.md)

# Robots
![Robots](/docs/images/robots.jpg)

This folder contains the actual robot assets for Franka Panda arm, Dynarm, Anymal, and ALMA. It also has the Robot manager script which allows for swapping out different robot assets as well as storing the settings in the player prefs. 


## Adding New Robots

To add a new robot to the system you can either import an existing URDF or create a new model from scratch. The current import method is a bit clunky with complex robots that refence multiple packages. If you have a better solution, please let us know!

1. **Convert Xacro to URDF**: Use the [Xacro](https://wiki.ros.org/xacro) tool to convert your Xacro file to a single URDF file. Usually this can be done with the command:
   ```bash
   rosrun xacro xacro --inorder -o <your_robot>.urdf <your_robot>.xacro
   ```

2. **Add the URDF Importer Package**: Add the [Unity URDF Importer](https://github.com/Unity-Technologies/URDF-Importer) according to the instructions in the repository. This package sometimes causes issues with building to device so if it is no longer needed for your project you can remove it.

3. **Copy Over Files**: The actual importing of the meshes can be tricky as ROS package lookups do not work in Unity. Instead it is recommended to make a working folder where you can copy any neccessary description/urdf packages along with the URDF file. An example of this would be 
``` 
    Assets/temp_urdf
    ├── <your_robot>.urdf
    ├── <your_robot>_description_package
    │   ├── meshes
    │   │   ├── <your_robot>.dae
    │   │   ├── <your_robot>_link.dae
```

4. **Import the URDF**: Once the files are copied over you can right click on the URDF file and select `Import Robot from Selected URDF File`. This will spawn a new Import Settings window where you then select `Import URDF`. Any missing packages/meshes will be reported in a popup window and can be copied into your working folder. It will likely take a few tries to get all the files in the right place, with the Import Settings window displaying how many links have been loaded at the bottom.

5. **Remove Simulation Components**: If the import is successful it will be added to the scene with the name of the robot. This robot is meant to work with Unity's ROS simulation system and includes a lot of components we do not need for teleoperation. To remove these add the [`URDF Converter`](/home/maximum/unity/unity_ros_teleoperation/Assets/Components/Robots/Scripts/URDFConverter.cs) component to the robot GameObject and press `Convert`. This will remove extra components and GameObjects as well as set the TF Attachment for each link to track the TF tree based on GameObject names. This means the root node of the robot's tf tree will be set to the name of the robot GameObject.

6 . **Add the Robot Manager**: Once the robot been cleaned up, drag the GameObject into the Robots > Prefabs folder to create a robot prefab. The robot prefab in the scene can now be deleted. This prefab can then be added to the `Model Manager` in palmmenu > MenuCanvas > Settings. Here the robot can be added to the Robots array with a custom name, and the prefab can be placed in the `Model Root` field. This will automatically add it to the drop down of robots.

![Model Manager](/docs/images/model_manager.png)

7. **Cleanup**: Lastly, since the prefab now links the UUIDs of the meshes instead of the paths, they can be moved to a folder in the Robots > Meshes. To select all the meshes in the prefab, right click on the prefab and select `Select Dependancies`. This will show all the components referenced by the prefab, you then need to select only the meshes and materials that are unique to the robot and drag these into the meshes folder. You should now be able to delete the working folder and have a cleaned robot prefab.

8. **[Optional] Clean the Meshes**: If your meshes are too high resolution you may have major performance hits when running on headset. The URDF Converter script has a button `Count` which will print out details on how many vertices and triangles are in the meshes. It's recommended to keep the meshes under 300k vertices and 200k triangles. After step 7, you can open the meshes in your new Robots > Meshes folder and edit the meshes in your tool of choice. The recommended tool is [Blender](https://www.blender.org/) which is free and open source. You can use the `Decimate` modifier to reduce the number of vertices in the mesh, or `Limited Dissolve` in the vertex delete menu. These are only starting suggestions, there are numerous online tools and tutorials on simplifiying your meshes.