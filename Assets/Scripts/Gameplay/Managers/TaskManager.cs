using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Manages player tasks, assigns a set amount of tasks to each player, manages completion of them.
/// 
/// Uses NetworkedVar to sync status about how many tasks are completed etc.
/// A task is a trigger, possible answers and a correct answer. For demonstration its simple addition-math. We send all players only the possible-answers
/// </summary>
class TaskManager : NetworkedBehaviour
{
    public static TaskManager Instance { get; private set; }

    [SerializeField] private PlayerInteractInTrigger[] taskLocations;

    //Key is player-id, value is location-index/answer
    private Dictionary<ulong, Dictionary<int, int>> serverAnswers;

    void OnEnable() => Instance = this;
    void OnDisable() => Instance = null;

    public override void NetworkStart()
    {
        base.NetworkStart();

        if (!IsServer) return;

        //Lets give tasks to everyone, that isnt an imposter
        foreach (var p in PlayerManager.Instance.Players)
        {
            //Is imposter, dont give task
            if (LobbyManager.Instance.ServerImposters.Contains(p.NetworkPlayer)) continue;

            serverAnswers.Add(p.NetworkPlayer.ID, new Dictionary<int, int>());

            //NetworkedVars are only checked and synced once per frame, this wont send more packages
            TotalTaskCount.Value += LobbyManager.Instance.GameSettings.Value.TaskCount;



            var shuffledLocations = taskLocations.OrderBy(t => Random.value);
            var it = shuffledLocations.GetEnumerator();


            for (int i = 0; i < LobbyManager.Instance.GameSettings.Value.TaskCount; i++)
            {
                it.MoveNext(); //we are assuming there are more locations than tasks. 

                var question = string.Empty;
                var correctAnswer = 0;
                var answers = new int[ANSWER_COUNT];

                switch (Random.Range(0, 3))
                {
                    case 0: //add
                        {
                            int a = Random.Range(2, 20),
                                b = Random.Range(5, 30);
                            question = $"{a} + {b} = ?";


                            //First we add the correct answer, then we loop through all answers generating a random one.
                            //If it doesnt exist yet, add it. If it does, try again. Hope we dont end up with an inf loop

                            int correctIndex = Random.Range(0, ANSWER_COUNT);
                            answers[correctIndex] = correctAnswer = a + b;

                            for (int j = 0; j < answers.Length; j++)
                            {
                                if (j == correctIndex) continue;

                                int answer = Mathf.RoundToInt(Random.Range((a + b) * .5f, (a + b) * 1.5f));

                                if (!answers.Contains(answer))
                                    answers[j] = answer;
                                else
                                    j--;
                            }

                        }
                        break;
                    case 1: //sub
                        {
                            int a = Random.Range(2, 20),
                                b = Random.Range(5, 30);
                            question = $"{a} - {b} = ?";

                            //First we add the correct answer, then we loop through all answers generating a random one.
                            //If it doesnt exist yet, add it. If it does, try again. Hope we dont end up with an inf loop

                            int correctIndex = Random.Range(0, ANSWER_COUNT);
                            answers[correctIndex] = correctAnswer = a - b;

                            for (int j = 0; j < answers.Length; j++)
                            {
                                if (j == correctIndex) continue;

                                int answer = Mathf.RoundToInt(Random.Range((a - b) * .5f, (a + b) - 1.5f));

                                if (!answers.Contains(answer))
                                    answers[j] = answer;
                                else
                                    j--;
                            }

                        }
                        break;
                    case 2: //mul
                        {
                            int a = Random.Range(2, 10),
                                b = Random.Range(2, 10);
                            question = $"{a} * {b} = ?";


                            //First we add the correct answer, then we loop through all answers generating a random one.
                            //If it doesnt exist yet, add it. If it does, try again. Hope we dont end up with an inf loop

                            int correctIndex = Random.Range(0, ANSWER_COUNT);
                            answers[correctIndex] = correctAnswer = a * b;

                            for (int j = 0; j < answers.Length; j++)
                            {
                                if (j == correctIndex) continue;

                                int answer = Mathf.RoundToInt(Random.Range((a + b) * .5f, (a + b) * 1.5f));

                                if (!answers.Contains(answer))
                                    answers[j] = answer;
                                else
                                    j--;
                            }

                        }
                        break;
                }

                var task = new TaskData()
                {
                    LocationIndex = System.Array.IndexOf(taskLocations, it.Current),
                    Answers = answers,
                    Question = question
                };
                p.NetworkController.Tasks.Add(task);
                serverAnswers[p.NetworkPlayer.ID].Add(task.LocationIndex, correctAnswer);
            }

        }
    }
    const int ANSWER_COUNT = 3;
    public struct TaskData : IBitWritable
    {
        public int LocationIndex;
        public string Question;
        public int[] Answers;

        public void Read(Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                LocationIndex = reader.ReadByte();
                Question = reader.ReadString().ToString();
                reader.ReadIntArrayPacked(Answers, ANSWER_COUNT);
            }
        }

        public void Write(Stream stream)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteByte((byte)LocationIndex);
                writer.WriteString(Question);
                writer.WriteIntArrayPacked(Answers, ANSWER_COUNT);
            }
        }
    }


    //Everyone may know the total tasks, but only the Server may write to it
    public NetworkedVar<int> TotalTaskCount = new NetworkedVar<int>(new NetworkedVarSettings()
    {
        ReadPermission = NetworkedVarPermission.Everyone,
        WritePermission = NetworkedVarPermission.ServerOnly
    });
    public NetworkedVar<int> CompletedTaskCount = new NetworkedVar<int>(new NetworkedVarSettings()
    {
        ReadPermission = NetworkedVarPermission.Everyone,
        WritePermission = NetworkedVarPermission.ServerOnly
    });


    public async Task<bool> BroadcastValidateAnswer(TaskData task, int answer)
    {
        //Uses included TaskUtils to wait for an answer
        //Not sure if RPCResponse is going to be included in the Unity Multiplayer API, this is kind of a nice usecase
        var response = await InvokeServerRpc(serverValidateAnswer, task.LocationIndex, answer);

        return response.Value;
    }

    [ServerRPC(RequireOwnership = false)]
    private bool serverValidateAnswer(int locationIndex, int answer)
    {
        bool isCorrect = serverAnswers[ExecutingRpcSender][locationIndex] == answer;

        if (isCorrect)
            CompletedTaskCount.Value++;

        return isCorrect;
    }
}