# UniteUs

This is [my](https://tjheuvel.net/) personal test project to see what pre-Unity MLAPI looks like and how it works. 

I have modified it slightly to support my own way of managing networked objects. By default in MLAPI you have to use a prefab based workflow, and call some Spawn methods on the server. This is so it can manage the ids for each object itself, we need a unique ID per network object that is send over the network, otherwise we dont know where to send a message too.

I prefer to manage my own objects, and assign network ids manually. For objects in the scene i serialize a network-id, for players i manually assign them based on the player id. 

Other than that i tried to vary in the ways i use the features provided by MLAPI, as an example on how to use NetworkVar and RPC's.   

Its made with Unity 2020.1.6f1, but should run on most versions. I copied over the source code of MLAPI and added assembly definition files, instead of using the DLL provided in the package, so i can make my own customizations. You can test by building and running the project and opening multiple instances, and connect with the Unity Editor too.

The idea is that you can join a lobby by IP address, but instead of sending IP addresses over i encode them to a human readable-ish string. That is the lobby id. I have only tested Join Local.

Assets by the talented Asset Jesus; [Kenney](http://www.kenney.nl).
