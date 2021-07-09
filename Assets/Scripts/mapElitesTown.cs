using System.Collections;
using System.Collections.Generic;


//Class for storing a town in 2D matrix form, along with details about it
public class mapElitesTown
{
    private int[,] townRepresentation;

    private int fitness;
    private int streetCount;
    private int totalHeight;
    private int maxHeight;

    private ArrayList buildingList = new ArrayList();

    public mapElitesTown(int[,] townRep){
        townRepresentation =(int[,]) townRep.Clone();

        calculateMetrics(townRep);
    }

    //Constructor for when we have a list of buildings, 
    public mapElitesTown(int[,] townRep, ArrayList buildingList){
        townRepresentation =(int[,]) townRep.Clone();
        this.buildingList = buildingList;

        calculateMetrics(townRep);
    }
    //Calculate metrics for the town chunk, both fitness and behevioral features
    private void calculateMetrics(int[,] townRep){
        int localFitness = 0;
        int localStreetCount = 0;
        int localTotalHeight = 0;
        int localMaxHeight = 0;
        for (int x = 0; x<townRep.GetLength(0); x++){
            for (int y = 0; y<townRep.GetLength(1); y++){
                int cellValue = townRep[x,y];

                if (cellValue > localMaxHeight){
                    localMaxHeight = cellValue;
                }

                if (cellValue == 0){
                    localStreetCount +=1;
                }
                else{
                    localTotalHeight+=cellValue;
                }

                //Check each of the four adjacent cells (if they arent outside our grid)
                //If they are the same, increase fitness
                //For each matching cell, it increases the local fitness by 2, this hopefully will extra priviledge clusters
                //i.e two pairs of matching size blocks in different parts of the level is much less good than 4 together
                int tileFitness = 1;

                //Check if there is space in different directions (and that were not at the edge)
                bool northFree = ((y+1)<townRep.GetLength(1));
                bool southFree = ((y-1)>=0);
                bool westFree = ((x-1)>=0);
                bool eastFree = ((x+1)<townRep.GetLength(0));

                if (westFree){
                    if (townRep[x-1,y]==cellValue){
                        tileFitness*=2;
                    }
                }
                if (eastFree){
                    if (townRep[x+1,y]==cellValue){
                        tileFitness*=2;
                    }
                }
                if (southFree){
                    if (townRep[x,y-1]==cellValue){
                        tileFitness*=2;
                    }
                }
                if (northFree){
                    if (townRep[x,y+1]==cellValue){
                        tileFitness*=2;
                    }
                }
                if (northFree&&westFree){
                    if (townRep[x-1,y+1]==cellValue){
                        tileFitness*=2;
                    }                    
                }
                if (northFree&&eastFree){
                    if (townRep[x+1,y+1]==cellValue){
                        tileFitness*=2;
                    }     
                }
                if (southFree&&westFree){
                    if (townRep[x-1,y-1]==cellValue){
                        tileFitness*=2;
                    }                    
                }
                if (southFree&&eastFree){
                    if (townRep[x+1,y-1]==cellValue){
                        tileFitness*=2;
                    }                    
                }                

                localFitness+=tileFitness;                                                
            }
        }

        this.fitness = localFitness;
        this.streetCount = localStreetCount;
        this.totalHeight = localTotalHeight;
        this.maxHeight = localMaxHeight;

    }

    public mapElitesTown Clone(){
        return new mapElitesTown(townRepresentation);
    }

    public int[,] getRepresentation(){
        return townRepresentation;
    }
    
    public int getFitness(){
        return fitness;
    }

    public int getStreetCount(){
        return streetCount;
    }

    public int getTotalHeight(){
        return totalHeight;
    }

    public int getMaxHeight(){
        return maxHeight;
    }

    //Update the xy offsets of every building stored with this chunk
    public void updateBuildingOffset(int[] offset){
        foreach(building building in buildingList){
            building.setxyOffset(offset);
        }
    }

    public ArrayList getBuildingList(){
        return buildingList;
    }


}
