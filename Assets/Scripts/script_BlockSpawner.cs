using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class script_BlockSpawner : MonoBehaviour
{
    public GameObject basicBlock;

    public GameObject basicRoof;

    //Takes in binary 3D matrix and generates block city
    public void spawnBlocks(int[,,] inputMatrix){

        Vector3 defaultSpawnPosition = this.transform.position;

        for (int x = 0; x<inputMatrix.GetLength(0); x++){
            for (int y = 0; y<inputMatrix.GetLength(1); y++){
                for (int z = 0; z<inputMatrix.GetLength(2); z++){
                    //Debug.Log("Checking cell: " + x + "," + y + "," + z);
                    //Debug.Log("Cell value: " + inputMatrix[x,y,z]);
                    
                    //Get spawn position
                    Vector3 spawnPos = new Vector3(defaultSpawnPosition.x+x, defaultSpawnPosition.y+y, defaultSpawnPosition.z+z);

                    //If we find a 1 in our matrix, spawn a block at our position, offset by its matrix position
                    if (inputMatrix[x,y,z] == 1){
                        Instantiate(basicBlock, spawnPos, this.transform.rotation, this.transform);
                    }
                    else if (inputMatrix[x,y,z] == 2){
                        Instantiate(basicRoof, spawnPos, this.transform.rotation, this.transform);

                    }

                }
            }
        }
    }

    //Takes in a 2D int matrix where each int represents 
    public void spawnBlocks2D(int[,] inputMatrix, int xOffset, int zOffset){

        Vector3 defaultSpawnPosition = this.transform.position;

        for (int x = 0; x<inputMatrix.GetLength(0); x++){
            for (int z = 0; z<inputMatrix.GetLength(1); z++){
                
                //Debug.Log("Curr cell value: " + inputMatrix[x,z]);
                
                //Check if there is a building at the current location
                if (inputMatrix[x,z]>0){
                    //Debug.Log("Spawning " +  inputMatrix[x,z] + " blocks");
                    for (int y = 0; y<inputMatrix[x,z]; y++){

                        //Get spawn position
                        Vector3 spawnPos = new Vector3(defaultSpawnPosition.x+x+xOffset, defaultSpawnPosition.y+y, defaultSpawnPosition.z+z+zOffset);

                        Instantiate(basicBlock, spawnPos, this.transform.rotation, this.transform);
                    }
                }
            }
        }
    }

}
