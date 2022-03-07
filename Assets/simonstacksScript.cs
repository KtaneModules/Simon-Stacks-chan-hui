using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class simonstacksScript : MonoBehaviour
{
	class Hex
	{
		public int[] hexes = new int[19]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

		public Hex()
		{
		}
		
		public Hex(string values)
		{
			for (int i = 0; i < 19; i++)
			{
				this.hexes[i] = values[i] - '0';
			}
		}

		public static Hex operator ^(Hex a, Hex b)
		{
			Hex temp = new Hex();
			for (int i = 0; i < 19; i++)
			{
				temp.hexes[i] = a.hexes[i] ^ b.hexes[i];
			}
			return temp;
		}
		
		public static bool operator ==(Hex a, Hex b)
		{
			for (int i = 0; i < 19; i++)
			{
				if (a.hexes[i] != b.hexes[i])
				{
					return false;
				}
			}
			return true;
		}
		
		public static bool operator !=(Hex a, Hex b)
		{
			for (int i = 0; i < 19; i++)
			{
				if (a.hexes[i] != b.hexes[i])
				{
					return true;
				}
			}
			return false;
		}
	}

	public KMAudio audio;
	public KMBombInfo bomb;

	public KMSelectable[] hexes;
	public Material[] hexMats;
	public KMSelectable background;

	private List<Hex> Flashes = new List<Hex>();
	private List<Hex> Solutions = new List<Hex>();
	private List<String> Colors = new List<String>();

	private String[] RawFlash = new string[] {"0001100111001100000", "0000000011001110110", "0000000001100111011", "0000011001110011000", "0110111001100000000", "1101110011000000000", "1111001100011001111", "0000110010100110000"};
	private String[] SolutionR = new string[] {"1010000100010000101", "0100110101010110010", "0100010011001011000", "0011111000001111100", "1110101111110101111", "1110100100110100111", "0010011001110000000", "1111111110111111111"};
	private String[] SolutionG = new string[] {"1100010110110100011", "1100100001111101100", "1110000110110000111", "0000011010101100000", "0010010001000100100", "1010110000000110101", "0000100001100110000", "0110011000110011011"};
	private String[] SolutionB = new string[] {"1011110101111110101", "1110011001010100100", "0111000100010001110", "0000110011100110000", "0100010110000010010", "0100101100100100101", "1101101000111101110", "1010101010101010101"};
	private String[] SolutionY = new string[] {"1100000000111100000", "1001100111111100100", "0010101101000101001", "1010110001000110101", "1111011101011101111", "0110001001001000110", "1000001001001000001", "0111001001001001110"};

	private Hex SubmittedHex = new Hex();
	
	private bool SubmissionMode = false;
	private bool ShouldSound = false;
	private int CurrentStage = 1;
	private int TotalStage = 3;
	
	//logging
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved = false;
	
	public string TwitchHelpMessage = "Use !{0} A1 C5 E2 to press A1, C5 and E2(A-E for column, numerals for row).  Use !{0} go or !{0} submit to press the background.";


	void Awake()
	{
		moduleId = moduleIdCounter++;
		background.OnInteract += delegate() { backgroundPrsss(); return false; };
		foreach (var hex in hexes){
			KMSelectable pressedhex = hex;
			for (int i=0;i<hexes.Length;i++){
				if (pressedhex == hexes[i].GetComponent<KMSelectable>()){
					pressedhex.OnInteract += delegate () { hexPress(pressedhex, i); return false; };
					break;}}
		}
	}

	void Start ()
	{
		GetStages();
		StartCoroutine(FlashingHexes());
		Debug.LogFormat("[Simon Stacks #{0}] Stage {1} - Flashing {2} as {3}, Solution is {4}", moduleId, CurrentStage, HexToString(Flashes[CurrentStage - 1]), Colors[CurrentStage - 1], HexToString(GetSolution(CurrentStage)));
	}

	void GetStages()
	{
		TotalStage = UnityEngine.Random.Range(3, 6);
		Debug.LogFormat("[Simon Stacks #{0}] Generating {1} Stages...", moduleId, TotalStage);
		for (int i = 0; i < TotalStage; i++)
		{
			Hex tempflash = new Hex();
			Hex tempSolution = new Hex();

			List<int> raws = new List<int>(new int[] {0, 1, 2, 3, 4, 5, 6, 7});
			raws.Shuffle();
			int color = UnityEngine.Random.Range(0, 4);
			Colors.Add((new string[] {"Red", "Green", "Blue", "Yellow"})[color]);
			for (int j = 0; j < UnityEngine.Random.Range(1, 9); j++)
			{
				tempflash ^= new Hex(RawFlash[raws[j]]);
				if (color == 0)
				{
					tempSolution ^= new Hex(SolutionR[raws[j]]);
				}
				else if (color == 1)
				{
					tempSolution ^= new Hex(SolutionG[raws[j]]);
				}
				else if (color == 2)
				{
					tempSolution ^= new Hex(SolutionB[raws[j]]);
				}
				else if (color == 3)
				{
					tempSolution ^= new Hex(SolutionY[raws[j]]);
				}
			}
			Flashes.Add(tempflash);
			Solutions.Add(tempSolution);
		}
	}

	IEnumerator FlashingHexes()
	{
		for (int i = 0; i < CurrentStage; i++)
		{
			for (int j = 0; j < 19; j++)
			{
				if (Flashes[i].hexes[j] == 1)
				{
					hexes[j].GetComponent<MeshRenderer>().material 
						= hexMats[Array.IndexOf((new String[] {"Red", "Green", "Blue", "Yellow"}), Colors[i])];
				}
			}

			if (ShouldSound)
			{
				if (Colors[i] == "Red")
				{
					audio.PlaySoundAtTransform("SFXR", transform);
				}
				else if (Colors[i] == "Green")
				{
					audio.PlaySoundAtTransform("SFXG", transform);
				}
				else if (Colors[i] == "Blue")
				{
					audio.PlaySoundAtTransform("SFXBNew", transform);
				}
				else if (Colors[i] == "Yellow")
				{
					audio.PlaySoundAtTransform("SFXY", transform);
				}
			}

			for (int sec = 0; sec < 40; sec++)
			{
				if (!SubmissionMode)
				{
					yield return new WaitForSeconds(.01f);
				}
			}
			foreach (var hex in hexes)
			{
				hex.GetComponent<MeshRenderer>().material = hexMats[4];
			}
			for (int sec = 0; sec < 20; sec++)
			{
				if (!SubmissionMode)
				{
					yield return new WaitForSeconds(.01f);
				}
			}
		}
		for (int sec = 0; sec < 30; sec++)
		{
			if (!SubmissionMode)
			{
				yield return new WaitForSeconds(.01f);
			}
		}

		if (!SubmissionMode)
		{
			StartCoroutine(FlashingHexes());
		}
	}

	void hexPress(KMSelectable hex, int indv)
	{
		if (moduleSolved || !SubmissionMode){
			return;
		}
		
		hex.AddInteractionPunch(.05f);

		if (SubmittedHex.hexes[indv] == 0)
		{
			audio.PlaySoundAtTransform("hexPressNew", transform);
			hexes[indv].GetComponent<MeshRenderer>().material = hexMats[5];
			SubmittedHex.hexes[indv] = 1;
		}
		else
		{
			audio.PlaySoundAtTransform("hexReleaseNew", transform);
			hexes[indv].GetComponent<MeshRenderer>().material = hexMats[4];
			SubmittedHex.hexes[indv] = 0;
		}
	}

	void backgroundPrsss()
	{
		if (moduleSolved){
			return;
		}
		
		background.AddInteractionPunch(.25f);
		SubmissionMode = !SubmissionMode;
		if (SubmissionMode)
		{
			audio.PlaySoundAtTransform("Enter", transform);
			SubmittedHex = new Hex();
		}
		else
		{
			ShouldSound = true;
			foreach (var hex in hexes)
			{
				hex.GetComponent<MeshRenderer>().material = hexMats[4];
			}
			if (SubmittedHex == GetSolution(CurrentStage))
			{
				Debug.LogFormat("[Simon Stacks #{0}] You Submitted {1}, Which is right!", moduleId, HexToString(SubmittedHex));
				CurrentStage++;
				if (CurrentStage > TotalStage)
				{
					GetComponent<KMBombModule>().HandlePass();
					audio.PlaySoundAtTransform("Solved", transform);
					moduleSolved = true;
				}
				else
				{
					audio.PlaySoundAtTransform("Stage", transform);
					Debug.LogFormat("[Simon Stacks #{0}] Stage {1} - Flashing {2} as {3}, Solution is {4}", moduleId, CurrentStage, HexToString(Flashes[CurrentStage - 1]), Colors[CurrentStage - 1], HexToString(GetSolution(CurrentStage)));
				}
			}
			else
			{
				GetComponent<KMBombModule>().HandleStrike();
				Debug.LogFormat("[Simon Stacks #{0}] You Submitted {1}, Expecting {2}", moduleId, HexToString(SubmittedHex), HexToString(GetSolution(CurrentStage)));
			}

			if (!moduleSolved)
			{
				StartCoroutine(FlashingHexes());
			}
		}
	}

	String HexToString(Hex hex)
	{
		String temp = "(";
		String[] coords = new string[]
		{
			"A1", "A2", "A3",
			"B1", "B2", "B3", "B4",
			"C1", "C2", "C3", "C4", "C5",
			"D1", "D2", "D3", "D4",
			"E1", "E2", "E3"
		};
		for (int i = 0; i < 19; i++)
		{
			if (hex.hexes[i] == 1)
			{
				if (temp != "(")
				{
					temp += ", ";
				}
				temp += coords[i];
			}
		}
		temp += ")";
		return (temp);
	}
	Hex GetSolution(int stage)
	{
		Hex temp = new Hex();
		for (int i = 0; i < stage; i++)
		{
			temp ^= Solutions[i];
		}
		return temp;
	}

	public IEnumerator ProcessTwitchCommand(string command)
	{

		string[] cutInBlank = command.Split(new char[] {' '});

		if (cutInBlank.Length == 1 && (cutInBlank[0].Equals("GO", StringComparison.InvariantCultureIgnoreCase) 
		                               || cutInBlank[0].Equals("SUBMIT", StringComparison.InvariantCultureIgnoreCase)))
		{
			background.OnInteract();
			yield return null;
		}
		else
		{
			bool commandvalid = true;
			Dictionary<String, KMSelectable> Coordinates = new Dictionary<string, KMSelectable>() {{"A1", hexes[0]}, {"a1", hexes[0]}, {"A2", hexes[1]}, {"a2", hexes[1]}, {"A3", hexes[2]}, {"a3", hexes[2]}, {"B1", hexes[3]}, {"b1", hexes[3]}, {"B2", hexes[4]}, {"b2", hexes[4]}, {"B3", hexes[5]}, {"b3", hexes[5]}, {"B4", hexes[6]}, {"b4", hexes[6]}, {"C1", hexes[7]}, {"c1", hexes[7]}, {"C2", hexes[8]}, {"c2", hexes[8]}, {"C3", hexes[9]}, {"c3", hexes[9]}, {"C4", hexes[10]}, {"c4", hexes[10]}, {"C5", hexes[11]}, {"c5", hexes[11]}, {"D1", hexes[12]}, {"d1", hexes[12]}, {"D2", hexes[13]}, {"d2", hexes[13]}, {"D3", hexes[14]}, {"d3", hexes[14]}, {"D4", hexes[15]}, {"d4", hexes[15]}, {"E1", hexes[16]}, {"e1", hexes[16]}, {"E2", hexes[17]}, {"e2", hexes[17]}, {"E3", hexes[18]}, {"e3", hexes[18]}};
		
			foreach (var coords in cutInBlank)
			{ 
				if (!Coordinates.ContainsKey(coords)) 
				{ 
					commandvalid = false; 
				}
			}
		
			if (!SubmissionMode)
			{ 
				yield return "sendtochaterror You're not in submission mode yet.";
			}
			else
			{
				if (commandvalid)
				{
					foreach (var coords in cutInBlank)
					{ 
						Coordinates[coords].OnInteract();
						yield return new WaitForSeconds(.05f);
					}
					yield return null;
				}
				else 
				{ 
					yield return "sendtochaterror There is an invalid coordinate in command.";
				}
			}
		}
		
	}

	public IEnumerator TwitchHandleForcedSolve()
    {
		int start = CurrentStage;
		for (int i = start; i < TotalStage + 1; i++)
        {
			if (!SubmissionMode)
            {
				background.OnInteract();
				yield return new WaitForSeconds(.1f);
			}
			int[] answer = GetSolution(CurrentStage).hexes;
			for (int j = 0; j < answer.Length; j++)
            {
				if (SubmittedHex.hexes[j] != answer[j])
                {
					hexes[j].OnInteract();
					yield return new WaitForSeconds(.05f);
				}
            }
			background.OnInteract();
			yield return new WaitForSeconds(.1f);
		}
	}
}
