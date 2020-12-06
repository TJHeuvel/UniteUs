I'm a Unity developer with about 10 years of experience, and a lot of experience with Photon. Every different team, and project, requires a different team setup, and in my experience i think i know what works and what doesnt. I'd like to use the experience i have, but its hard in the MLAPI. It seems to force the object-lifecycle on you, and i dont think it maps well to different types of games.

- I really want to use the source code, not a DLL. It was a hassle to set that up, a separate UnityPackage would be nice. In the end i made assembly definition files for each folder, and removed Tests. 
- I then had to add warning ignores to RelayTransport.cs, UnetTransport.cs
- I dont know what a transport is, still i have to choose between some. I picked one at random. Initialisation
- It does a lot i dont want, and i need to do for some reason. NetworkingManager has things for PlayerPrefab, Scene names. I dont want that, i can do that myself. I wish it was more optional, e.g. in another networkmanager. I do see i can disable scene stuff at least
-Because of my weird setup of having source code i noticed NetworkingManagerEditor is not in the MLAPI namespace. If you ask me there should be an MLAPI.Editor
- I had something weird when making the networkingmanager a prefab. The transport wouldnt be linked
- Its hard not do it in the perfect MLAPI way, and its non obvious (for me) what that way is. I dont want to use managed prefabs, i want to instantiate my own things. 
NetworkManager manages way too much. 
- It would be really sweet if an RPC could be both Server and Client. I want to avoid differences there as much as possible, i think thats a good pattern. 
I’m very used to Relay, and i think there should be built in functionality for that. Clients sending message to client, via code. In practice that would loop through the server, which would immediately relay it to the players. In my ideal case there is only one InvokeRPC method, with an RPCTarget parameter of Server, Players, or player IDs. Or maybe because there is no ServerRPC method it is relayed, otherwise intercepted. With an option to send it to players specifically. At the moment this is a difficult concept to grasp.
- Why is there no connected callback? I have to wait for those tasks. Is there also no connected field? Is that isListening?
- An async ext methods for those tasks would be very nice
- Ive so far spend about one day getting a simple lobby to work. In Photon this is built in.
- My NetworkedList just doesnt work out of the box. I dont see a lot of documentation. It doesnt work because its not a spawnedobject, i dont see why i need that. I hacked the framework to expose VarUpdate…
 i have this in my scene, why do i need to spawn it? I could just set an id myself? I dont understand because in my first scene i have a similar setup and i do not need to spawn it, but the second scene i do? I want to manage the lifecycle of my objects myself. I made a SceneNetworkObject to manually call SpawnNetworkObjectLocally, that works. 
- I like syncvars! Didnt think i'd use them, but they are quite sweet
- The documentation is divided in several sections, i dont know when to look in advanced or in core concepts
- Networkvar serialization should be possible with new generic serialization?
- Why does BitReader.ReadByte return an int, not a byte?
- I find bitreader/writer confusing, i always forget that a single is a float. Why isnt it ReadFloat? But it is ReadFloatArray.
- RPC Methods now accept an array of ulong target, it would be nice if it was an IENumerable<ulong>. It would be more flexible
- My IDE is very slow because of the many rpc methods
- Object visibility is annoying. I dont understand why i get a warning about object visibility and NetworkedTransform doesnt 
- The name Singleton is weird. Instance says what it is, and is used by other unity things too.