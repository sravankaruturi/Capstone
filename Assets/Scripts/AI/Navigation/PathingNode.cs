﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathingNode : MonoBehaviour {

    // ఒక నోడుకి దాని ప్రక్కనే వున్న నోడుకీ మద్య దూరం 10 అని మనం అనుకుందాం, ఐమూలగా చూస్తే 14.
    // Let us assume that the distance between any two adjacent Nodes is 10 and that the Diagonal distance is 14.

    // ఇది మనం అన్ని నోడ్లు చూసుకొని పెట్టుకొంటాము.
    public int index;

    public Vector3 nodePosition;

    // ఇవి మనం రెండు నోడ్ల మద్యన దూరం కొలవటానికి.
    // These variables are going to be used to calculate the distance between two different nodes.
    public int gridX;
    public int gridY;

    // Neighbours.
    // ప్రక్కనే వున్నవి.
    [SerializeField]
    public List<PathingNode> connectedNodes;


    // A* కోసం కావలిసినవి
    // Pathing Variables
    public int gCost;
    public int hCost;

    // మనమీ నోడుకి ఎక్కడనుండి వచ్చామో అది.
    // The node from where you came to this node.
    [SerializeField]
    public PathingNode cameToThisNodeFrom;

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    private void Awake()
    {
        // ఇది అస్తమానూ మార్చము. అసలు మార్చకుండా చుడాలి.
        // We wouldn't change this often. Ideally, not at all.
        nodePosition = this.transform.position;
    }

    // Use this for initialization
    void Start () {

        // మనకి index అన్నదిమనంచేత్తోపెట్టేటట్టయితేచాలాకష్టంగావుంటుంది. అందుకనిమనంకోడుద్వారాకనుక్కుందాం.
        // The index is really hard to fill out so, I decided to make use of Grid X and Grid Y to create a unique index based on them.
        index = 1000 * gridY + gridX;

        // మనతోకలసివున్ననోడులనుకూడాఇలానేకనుక్కుంటాము.
        // The same goes for finding the neighbouring nodes.
        // This is a huge quality of life change for the developer at the expense of a huge performance hit at the begining of the game.
        foreach ( GameObject currentNode in GameObject.FindGameObjectsWithTag("PathNode"))
        {
            if ( ( 1 == Mathf.Abs(this.gridX - currentNode.GetComponent<PathingNode>().gridX) ) || (1 == Mathf.Abs(this.gridY - currentNode.GetComponent<PathingNode>().gridY)))
            {
                this.connectedNodes.Add(currentNode.GetComponent<PathingNode>());
            }
        }

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}