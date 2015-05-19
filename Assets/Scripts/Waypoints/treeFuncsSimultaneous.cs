#define includeTreeFuncsSimultaneous
#if includeTreeFuncsSimultaneous
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
public partial class Visibility1 : MonoBehaviour 
{
	Hashtable m_hCompleteNodeTable = new Hashtable();
	private int m_lastPathIndex = 3;
	private void DumpInfoFile(string dirName,float totalTime)
	{
		string sourceFileName = dirName+"\\Info"+".txt";
		StreamWriter sw = new StreamWriter(sourceFileName);
		sw.WriteLine("Scene Name = "+currSceneName+"");
		sw.WriteLine("Discrete rows & cols = "+discretePtsX+" X "+discretePtsZ+"");
		sw.WriteLine("Speed of the Player = "+speedPlayer+"");
		sw.WriteLine("Distance covered by the player = "+m_stepDistance+"");
		sw.WriteLine("Max speed of the Enemy = "+speedEnemy+"");
		sw.WriteLine("Max distance covered by the Enemy = "+standardMaxMovement+"");
		sw.WriteLine("Time taken to calculate tree structure = "+totalTime+" mins"+"");
		sw.WriteLine("Current Date = " + System.DateTime.Now.ToLongDateString()+". Time = "+ System.DateTime.Now.ToLongTimeString() + "");
		sw.Close ();
	}
	private void DumpEdgesForLevel(Hashtable h_mapPtToNode,int levelOfAccess,string dirName)
	{
		string sourceFileName = dirName+"\\Edges"+levelOfAccess+".txt";
		StreamWriter sw = new StreamWriter(sourceFileName);
		//sw.WriteLine("(Vector3;level)|(Vector3;level)"+"");
		foreach(Vector3 vect in h_mapPtToNode.Keys)
		{
			NodeShadow nodeNow = (NodeShadow)h_mapPtToNode[vect];
			foreach(NodeShadow nodeNowParent in nodeNow.getParent())
			{
				sw.Write("("+nodeNow.getPos()+";"+nodeNow.getSafetyLevel()+")|("+nodeNowParent.getPos()+";"+nodeNowParent.getSafetyLevel()+")");
				sw.WriteLine("");
			}
		}
		sw.Close ();
	}
	public bool m_bContinueExecuteTrueCase = false;
	private void executeTrueCase2()
	{
		setGlobalVars1();
		standardMaxMovement = speedEnemy*(m_stepDistance/speedPlayer);
		Debug.Log ("Initialize standardMaxMovement = " + standardMaxMovement);
		float startTime = Time.realtimeSinceStartup;
		string dirName = createSaveDataDir(Application.dataPath);


		if(m_bContinueExecuteTrueCase)
			continueExecuteTrueCase (dirName);
		else
			executeTrueCaseFor2(dirName);
		
		float totalTime = (Time.realtimeSinceStartup - startTime)/60;
		Debug.Log("executeTrueCase Finished. Time taken is = "+totalTime+" mins");
		DumpInfoFile (dirName,totalTime);

	}
	private void executeTrueCaseFor2(string dirName)
	{
		Hashtable h_mapPtToNode = new Hashtable();
		int levelOfAccess = 0;
		List<NodeShadow> nodeSafeLevelNow = new List<NodeShadow> ();
		int j1=0;
		for(float j=m_minX;j<m_maxX && j1<discretePtsX;j+=m_step)
		{
			int k1=0;
			for(float k=m_minZ;k<m_maxZ && k1<discretePtsZ;k+=m_step)
			{
				//Debug.Log(j1+" , "+k1);
				Vector3 pt = new Vector3(j,1,k);
				if(!pointInShadow(pt,0))
				{
					k1++;
					continue;
				}
				Vector2 keyTemp = new Vector2(j1,k1);
				pt = (Vector3)h_mapIndxToPt[keyTemp];
				NodeShadow headNode = new NodeShadow (pt);
				headNode.setSafetyLevel (levelOfAccess);
				nodeSafeLevelNow.Add (headNode);
				k1++;
			}
			j1++;
		}
		int numOfLevels = m_lastPathIndex;/*pathPoints.Count-1*/;
		while(levelOfAccess<numOfLevels)//TODO:think other exit cases
		{
			levelOfAccess++;
			
			foreach(NodeShadow node in nodeSafeLevelNow)
			{
				Vector2 indexOfPtTemp = (Vector2)h_mapPtToIndx[node.getPos()];
				reachableChildren2 (node,indexOfPtTemp,levelOfAccess,h_mapPtToNode);
			}
			nodeSafeLevelNow = new List<NodeShadow> ();
			foreach(Vector3 vect in h_mapPtToNode.Keys)
			{
				nodeSafeLevelNow.Add((NodeShadow)h_mapPtToNode[vect]);
			}
			DumpEdgesForLevel(h_mapPtToNode,levelOfAccess,dirName);
			h_mapPtToNode.Clear();

		}
	}
	private bool addPossibleChild2(Vector2 tempVect2,NodeShadow node,int pathPointIndx,Hashtable h_mapPtToNode)
	{
		//Debug.Log ("Possible Child 1 ="+(Vector3)h_mapIndxToPt [tempVect2]);
		if(h_mapIndxToPt.ContainsKey(tempVect2))
		{
			Vector3 tempVect3 = (Vector3)h_mapIndxToPt[tempVect2];
			//Vector4 tempVect4 = new Vector4(tempVect3.x,tempVect3.y,tempVect3.z,pathPointIndx);
			//Debug.Log("standardMaxMovement = "+standardMaxMovement);
			//Debug.Log("Possible Child 2 = "+tempVect3);
			if(pointInShadow(tempVect3,pathPointIndx) && Vector3.Distance(node.getPos(),tempVect3)<=standardMaxMovement)
			{
				NodeShadow nodeChild;
				if(h_mapPtToNode.ContainsKey(tempVect3))
				{
					nodeChild = (NodeShadow)h_mapPtToNode[tempVect3];
				}
				else
				{
					nodeChild = new NodeShadow(tempVect3);
					nodeChild.setSafetyLevel(pathPointIndx);
					h_mapPtToNode.Add(tempVect3,nodeChild);
				}
				node.addChild(nodeChild);
				Debug.Log(tempVect3+" added as child of "+node.getPos()+" Dist b/w them is "+Vector3.Distance(node.getPos(),tempVect3));
				return true;
			}
			else
			{
				Debug.Log(tempVect3+" cannot be added as child of "+node.getPos()+" Dist b/w them is "+Vector3.Distance(node.getPos(),tempVect3));
			}
		}
		return false;
	}
	private void reachableChildren2(NodeShadow node,Vector2 indexOfPt,int pathPointIndx,Hashtable h_mapPtToNode)
	{
		int rowJ = (int)indexOfPt.x;
		int colK = (int)indexOfPt.y;
		addPossibleChild2(indexOfPt,node,pathPointIndx,h_mapPtToNode);
		while(true)
		{
			bool bStillReachable=false;
			bool bRunAgain=false;
			rowJ--;
			colK--;
			int rowLen = ((int)indexOfPt.x - rowJ)*2 +1;
			//////////////////////////////////////////////////////////////////////&&&&&&&&&&&&&&&&&
			Vector2 testPt2D = new Vector2(rowJ+rowLen/2,colK);
			Vector3 testPt3D = new Vector3(0,0,0);
			bool bPtAssigned = false;
			if(h_mapIndxToPt.ContainsKey(testPt2D))
			{
				testPt3D = (Vector3)h_mapIndxToPt[testPt2D];
				bPtAssigned = true;
			}
			else
			{
				testPt2D = new Vector2(rowJ+rowLen/2,colK+rowLen-1);
				if(h_mapIndxToPt.ContainsKey(testPt2D))
				{
					testPt3D = (Vector3)h_mapIndxToPt[testPt2D];
					bPtAssigned = true;
				}
				else
				{
					testPt2D = new Vector2(rowJ+rowLen-1,colK+rowLen/2);
					if(h_mapIndxToPt.ContainsKey(testPt2D))
					{
						testPt3D = (Vector3)h_mapIndxToPt[testPt2D];
						bPtAssigned = true;
					}
					else
					{
						testPt2D = new Vector2(rowJ,colK+rowLen/2);
						if(h_mapIndxToPt.ContainsKey(testPt2D))
						{
							testPt3D = (Vector3)h_mapIndxToPt[testPt2D];
							bPtAssigned = true;
						}
					}
				}
			}
			if(!bPtAssigned)
			{
				Debug.LogError("All Possible points exhausted. No outcome. Breaking from loop");
				break;
			}
			////////////////////////////////////////////////////////////////////////&&&&&&&&&&&&&&&&;
			float testDist = Vector3.Distance(node.getPos(),testPt3D);
			//Debug.Log("testDist = "+testDist);
			if(testDist > standardMaxMovement)// || rowJ<0 || colK<0 || rowJ+rowLen>discretePtsX || colK+rowLen>discretePtsZ)
				break;
			//Debug.Log("rowJ = "+rowJ);
			//Debug.Log("colK = "+colK);
			//Debug.Log("rowLen = "+rowLen);
			for(int i1=rowJ;i1<rowJ+rowLen;i1++)
			{
				Vector2 tempVect2 = new Vector2(i1,colK);
				bStillReachable = addPossibleChild2(tempVect2,node,pathPointIndx,h_mapPtToNode);
				if(bStillReachable)
					bRunAgain=true;
				tempVect2 = new Vector2(i1,colK+rowLen-1);
				bStillReachable = addPossibleChild2(tempVect2,node,pathPointIndx,h_mapPtToNode);
				if(bStillReachable)
					bRunAgain=true;
			}
			for(int i2=colK+1;i2<colK+rowLen-1;i2++)
			{
				Vector2 tempVect2 = new Vector2(rowJ,i2);
				bStillReachable = addPossibleChild2(tempVect2,node,pathPointIndx,h_mapPtToNode);
				if(bStillReachable)
					bRunAgain=true;
				tempVect2 = new Vector2(rowJ+rowLen-1,i2);
				bStillReachable = addPossibleChild2(tempVect2,node,pathPointIndx,h_mapPtToNode);
				if(bStillReachable)
					bRunAgain=true;
			}
			
			
		}
		/*Debug.Log(node.getPos()+" has following children");
		string childrEn="";
		foreach(NodeShadow ch in node.getChildren())
		{
			childrEn+=ch.getPos()+" , ";
		}
		Debug.Log(childrEn);
		*/
		/*Vector3 pt = (Vector3)h_mapIndxToPt[indexOfPt];
		Vector4 pt4 = new Vector4(pt.x,pt.y,pt.z,pathPointIndx-1);
		if(!m_hCompleteNodeTable.ContainsKey(pt4))
		{
			m_hCompleteNodeTable.Add (pt4,node);
		}
		List<NodeShadow> newChildren = new List<NodeShadow> ();
		foreach(NodeShadow childTemp in node.getChildren())
		{
			pt = childTemp.getPos();
			pt4 = new Vector4(pt.x,pt.y,pt.z,pathPointIndx);
			if(!m_hCompleteNodeTable.ContainsKey(pt4))
			{
				newChildren.Add(childTemp);
			}
		}
		return newChildren;
		*/
	}

	private int readLastNodeOutput(Hashtable h_mapPtToNode)
	{
		string sourceDirName = EditorUtility.OpenFolderPanel("Please select data node dir", Application.dataPath,"");
		
		List<char> sep = new List<char>();
		sep.Add(',');
		sep.Add(' ');
		sep.Add(';');
		sep.Add('(');
		sep.Add(')');
		sep.Add('|');
		int levelOfAccess = -1;
		string sourceFileName = EditorUtility.OpenFilePanel("Please select data node dir", Application.dataPath,"");


		StreamReader sr = new StreamReader(sourceFileName);
		string str;// = sr.ReadLine();
		while(!sr.EndOfStream)
		{
			str = sr.ReadLine();
			
			string[] line1 = str.Split(sep.ToArray());
			Debug.Log(str);
			List<string> line = new List<string>();
			for(int i=0;i<line1.Length;i++)
			{
				if(line1[i]=="")
					continue;
				line.Add(line1[i]);
				//Debug.Log(line1[i]);
			}

			//Vector4 keyObj = new Vector4(float.Parse(line[0]),float.Parse(line[1]),float.Parse(line[2]),float.Parse(line[3]));
			Vector3 keyObj = new Vector4(float.Parse(line[0]),float.Parse(line[1]),float.Parse(line[2]));
			//Vector4 parentKeyObj = new Vector4(float.Parse(line[4]),float.Parse(line[5]),float.Parse(line[6]),float.Parse(line[7]));

			NodeShadow node = null;
			if(!h_mapPtToNode.ContainsKey(keyObj))
			{
				
				node = new NodeShadow(new Vector3(keyObj.x,keyObj.y,keyObj.z));
				node.setSafetyLevel((int)float.Parse(line[3]));
				levelOfAccess = node.getSafetyLevel();
				m_hCompleteNodeTable.Add(keyObj,node);
			}
		}
		return levelOfAccess;
	}
	private void continueExecuteTrueCase(string dirName)
	{
		float startTime = Time.realtimeSinceStartup;
		Hashtable h_mapPtToNode = new Hashtable ();
		int levelOfAccess = readLastNodeOutput(h_mapPtToNode);
		int numOfLevels = pathPoints.Count-1;
		List<NodeShadow> nodeSafeLevelNow = new List<NodeShadow> ();
		foreach(Vector3 vect in h_mapPtToNode.Keys)
		{
			nodeSafeLevelNow.Add((NodeShadow)h_mapPtToNode[vect]);
		}
		h_mapPtToNode.Clear ();
		while(levelOfAccess<numOfLevels)//TODO:think other exit cases
		{
			levelOfAccess++;
			
			foreach(NodeShadow node in nodeSafeLevelNow)
			{
				Vector2 indexOfPtTemp = (Vector2)h_mapPtToIndx[node.getPos()];
				reachableChildren2 (node,indexOfPtTemp,levelOfAccess,h_mapPtToNode);
			}
			nodeSafeLevelNow = new List<NodeShadow> ();
			foreach(Vector3 vect in h_mapPtToNode.Keys)
			{
				nodeSafeLevelNow.Add((NodeShadow)h_mapPtToNode[vect]);
			}
			DumpEdgesForLevel(h_mapPtToNode,levelOfAccess,dirName);
			h_mapPtToNode.Clear();
			
		}
	}


	private void displayPredictedPaths2()
	{
		float startTime = Time.realtimeSinceStartup;
		List<NodeShadow> headNodes = readNodeStructureFor2 ();
		Debug.Log ("Num of headNodes = "+headNodes.Count);
		return;
		int numOfLevels = m_lastPathIndex/*pathPoints.Count*/;
		foreach(NodeShadow headNode in headNodes)
		{
			int numLevelsReached = findFurthestPathPointReached(headNode);
			float greenNum = numLevelsReached/numOfLevels;
			float redNum = 1-greenNum;
			showPosOfPoint(headNode.getPos(),new Color(redNum,greenNum,0));
		}
	}

	/*private void displayPredictedPaths2()
	{
		float startTime = Time.realtimeSinceStartup;
		//readNodeStructureFor2();
		List<NodeShadow> headNodes = readNodeStructureFor2();
		Debug.Log ("Num Of Head Nodes = " + headNodes.Count);
		//foreach(NodeShadow headNode in headNodes)
		NodeShadow headNode = headNodes [10];
		{
			//NodeShadow headNode = (NodeShadow)m_hCompleteNodeTable[vect];
			List<NodeShadow> firstPath = quickShortestPathDetected (headNode);
			
			showPosOfPoint (firstPath [0].getPos (),Color.cyan);
			standardMaxMovement = speedEnemy*(m_stepDistance/speedPlayer);
			for(int i=1;i<firstPath.Count;i++)
			{
				float dist = Vector3.Distance(firstPath[i].getPos(),firstPath[i-1].getPos());
				
				//Debug.Log(firstPath[i].getPos()+" ;;;;;; Distance from previous "+firstPath[i-1].getPos()+" is "+dist);
				if(dist>standardMaxMovement)
					Debug.LogError("Dist b/w 2 points should not be greater than standardMaxMovement.");
				//showPosOfPoint (firstPath [i].getPos (),Color.cyan);
				Line l = new Line(firstPath[i].getPos(),firstPath[i-1].getPos());
				l.DrawVector(allLineParent);
			}
		}
		float totalTime = (Time.realtimeSinceStartup - startTime)/60;
		Debug.Log("Finished displayPredictedPaths2. Time took to calculate and show shortest path = "+totalTime+" minutes");
	}*/
	private List<NodeShadow> readNodeStructureFor2()
	{
		List<NodeShadow> headNodes = new List<NodeShadow> ();
		setGlobalVars1 ();
		string sourceDirName = EditorUtility.OpenFolderPanel("Please select data node dir", Application.dataPath,"");

		List<char> sep = new List<char>();
		sep.Add(',');
		sep.Add(' ');
		sep.Add(';');
		sep.Add('(');
		sep.Add(')');
		sep.Add('|');
		int levelOfAccess = 1;
		string sourceFileName = sourceDirName+"\\Edges"+levelOfAccess+".txt";
		FileInfo fInfo = new FileInfo(sourceFileName);
		if(!fInfo.Exists)
			return headNodes;
		StreamReader sr = new StreamReader(sourceFileName);
		string str;// = sr.ReadLine();
		while(true)
		{
			////////////////////////////////////////////////////////////////////////////////////////
			while(!sr.EndOfStream /*&& jk>0*/)
			{
				str = sr.ReadLine();
				
				string[] line1 = str.Split(sep.ToArray());
				Debug.Log(str);
				List<string> line = new List<string>();
				for(int i=0;i<line1.Length;i++)
				{
					if(line1[i]=="")
						continue;
					line.Add(line1[i]);
					//Debug.Log(line1[i]);
				}
				
				Vector4 parentKeyObj = new Vector4();
				Vector4 keyObj = new Vector4(float.Parse(line[0]),float.Parse(line[1]),float.Parse(line[2]),float.Parse(line[3]));

				/*if(keyObj.w==0.0f)//A head Node
				{
					NodeShadow headNode = new NodeShadow(new Vector3(keyObj.x,keyObj.y,keyObj.z));
					headNode.setSafetyLevel((int)keyObj.w);
					if(!m_hCompleteNodeTable.ContainsKey(keyObj))
					{
						m_hCompleteNodeTable.Add(keyObj,headNode);
					}
					//headNodes.Add(headNode);
					continue;
				}*/
				//else
				//{
				parentKeyObj = new Vector4(float.Parse(line[4]),float.Parse(line[5]),float.Parse(line[6]),float.Parse(line[7]));
				//}
				NodeShadow node = null;
				if(!m_hCompleteNodeTable.ContainsKey(keyObj))
				{
					
					node = new NodeShadow(new Vector3(keyObj.x,keyObj.y,keyObj.z));
					node.setSafetyLevel((int)keyObj.w);
					m_hCompleteNodeTable.Add(keyObj,node);
				}
				else
				{
					node = (NodeShadow)m_hCompleteNodeTable[keyObj];

				}

				NodeShadow parentNode = null;
				if(!m_hCompleteNodeTable.ContainsKey(parentKeyObj))
				{
					parentNode = new NodeShadow(new Vector3(parentKeyObj.x,parentKeyObj.y,parentKeyObj.z));
					parentNode.setSafetyLevel((int)parentKeyObj.w);
					m_hCompleteNodeTable.Add(parentKeyObj,parentNode);
				}
				else
				{
					parentNode = (NodeShadow)m_hCompleteNodeTable[parentKeyObj];
				}
				
				parentNode.addChild(node);
				if(levelOfAccess==1)
				{
					headNodes.Add(parentNode);
				}
				
				
			}
			//Debug.Log ("Number of nodes are  = " + m_hCompleteNodeTable.Keys.Count);
			sr.Close ();
			///////////////////////////////////////////////////////////////////////////////////////;
			levelOfAccess++;
			sourceFileName = sourceDirName+"\\Edges"+levelOfAccess+".txt";
			fInfo = new FileInfo(sourceFileName);
			if(!fInfo.Exists)
				break;
			sr = new StreamReader(sourceFileName);
		}
		return headNodes;
	}


	private int findFurthestPathPointReached (NodeShadow headNode)
	{
		int lastIndex = m_lastPathIndex;//pathPoints.Count - 1;
		int maxIndex = 0;
		List<NodeShadow> maxPathIndices = new List<NodeShadow> ();
		List<NodeShadow> currPathIndices = new List<NodeShadow> ();
		List<NodeShadow> stack = new List<NodeShadow> ();
		stack.Add (headNode);
		int topIndex = -1;
		while(stack.Count>0)
		{
			//pop the top
			topIndex = stack.Count-1;
			NodeShadow nodeTop = stack[topIndex];
			stack.RemoveAt(topIndex);
			if(nodeTop.getSafetyLevel()>maxIndex)
			{
				maxIndex = nodeTop.getSafetyLevel();
			}
			stack.AddRange(nodeTop.getChildren());
			if(maxIndex==lastIndex)
				break;


			
		}
		return maxIndex;
	}
}
#endif