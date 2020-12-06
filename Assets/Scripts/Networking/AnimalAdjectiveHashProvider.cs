using System;
using System.Net;
using System.Text;
using UnityEngine;

abstract class HashProvider : ScriptableObject
{
    public abstract string GetAddressFromHash(string hash);
    public abstract string GetHashFromAddress(string address);

    public string GetHashFromCurrentAddress()
    {
        IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

        foreach (var i in localIPs)
        {
            if (i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                return GetHashFromAddress(i.MapToIPv4().ToString());
            if (i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return GetHashFromAddress(i.ToString());
        }
        throw new System.Exception("You arent connected to the internet");
    }
}


/*
	Converts an ip4 address [0-255].[0-255].[0-255].[0-255] into a human readable hash in the form of [Adjective].[Animal].[Adjective].[Animal]

	TODO: I'd like to randomise the input text, so your hash changes every once in a while. Maybe based on the time on time.windows.com?
*/
[CreateAssetMenu(menuName = "UniteUs/Config/AnimalAdjectiveHashProvider")]
class AnimalAdjectiveHashProvider : HashProvider
{
    [SerializeField] private TextAsset fileAdjectives, fileAnimals;

    private string[] adjectives, animals;
    void OnEnable()
    {
        var newlines = new[] { "\n", "\r\n" }; //support both newline and carriage return newline. *Ding!*
        adjectives = fileAdjectives.text.Split(newlines, StringSplitOptions.RemoveEmptyEntries);
        animals = fileAnimals.text.Split(newlines, StringSplitOptions.RemoveEmptyEntries);
    }
    public override string GetAddressFromHash(string hash)
    {
        StringBuilder sb = new StringBuilder();

        int startCharIndex = 0,
            endCharIndex = 0;

        int i = 0;

        //Find words between capital letters
        while (true)
        {
            if (++endCharIndex < hash.Length && !char.IsUpper(hash[endCharIndex])) continue;
            
            string word = hash.Substring(startCharIndex, endCharIndex - startCharIndex);

            var collection = (i & 1) == 0 ? adjectives : animals;
            int index = Array.IndexOf(collection, word);
            index -= i * 255;

            sb.Append(index);
            if (i < 3)
                sb.Append('.'); //dont add a period at the end
            else
                break; //found enough

            startCharIndex = endCharIndex;
            i++;
        }

        if (i != 3) throw new ArgumentException("Invalid hash");

        return sb.ToString();
    }

    public override string GetHashFromAddress(string address)
    {
        StringBuilder sb = new StringBuilder();

        var addressParts = address.Split('.');

        for (int i = 0; i < addressParts.Length; i++)
        {
            int index = int.Parse(addressParts[i]); //todo: add error checking
            var collection = (i & 1) == 0 ? adjectives : animals;

            sb.Append(collection[index + i * 255]);
        }

        return sb.ToString();
    }
}