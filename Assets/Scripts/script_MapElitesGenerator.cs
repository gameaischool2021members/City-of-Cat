using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class script_MapElitesGenerator : MonoBehaviour
{

    private enum RUNTYPE{

        //Total height (total number of blocks used) vs street count (cells with no buildings)
        THvSC = 1,
        //Total height vs max height
        THvMH = 2
        
    }
    //Parameters

    //This parameter is a hacky way of letting us experiment with different behavioral metrics within MAP Elites
    //Determines how the 'checkBelongs()' method works
    private RUNTYPE currRunType = RUNTYPE.THvSC;

    // How many generative steps we do before termination
    private int stepCount = 50000;
    //Size of Map elites grid. Always a square n by n grid
    private int gridSize = 5;

    //Size of generated towns. Always a square n by n grid
    private int townSize = 10;


    //Maximum allowable building height
    private int maxBuildingHeight = 3;

    //Behavioral Metric Parameters
    private int minStreeTiles = 0;
    private int maxStreetTiles;

    private int minTotalHeight = 0;
    private int maxTotalHeight;


    private float tileMutateChance = 0.5f;
    private float tileDuplicateChance = 0.2f;

    private float crossoverChance = 0.2f;


    //Overload method for when we want to specify some parameters
    public mapElitesTown[,] runMapElites(int gridSize, int stepCount){

        this.gridSize = gridSize;
        this.stepCount = stepCount;

        return runMapElites();
    }


    //Main method for generating the output town
    public mapElitesTown[,] runMapElites(){


        //Set behavioral characteristics to reasonable values for the town size
        this.maxStreetTiles = (int)(townSize*townSize);
        this.maxTotalHeight = (int) (townSize*townSize)*maxBuildingHeight;



        mapElitesTown[,] mapElitesGrid = generateStartingGrid();

        if(currRunType == RUNTYPE.THvSC){
            Debug.Log("Run BCs: MinTH: " + minTotalHeight + " Max: " + maxTotalHeight + ". MinSC: " + minStreeTiles + " Max: " + maxStreetTiles);
        }
        

        //MAP Elites core loop#
        
        for (int i = 0; i <stepCount; i++){
            
            if (i % 100 == 0){
                Debug.Log("Step count: " + i + ". Current populated cells: " + getPopulatedCellCount(mapElitesGrid) + " Current Total Fitness: " + getTotalGridFitness(mapElitesGrid));
            }

            mapElitesTown currTown1 = getRandomLevelFromGrid(mapElitesGrid);
            mapElitesTown currTown2 = getRandomLevelFromGrid(mapElitesGrid);
            //Mutate it
            mapElitesTown newTown1 = tileMutate(currTown1);
            mapElitesTown newTown2 = tileMutate(currTown2);
            //Tile duplicate mutate it
            newTown1 = tileDuplicate(newTown1);
            newTown2 = tileDuplicate(newTown2);

            //Crossover chance
            if (Random.Range(0f, 1f)<crossoverChance){
                mapElitesTown[] output = crossover(newTown1, newTown2);
                newTown1 = output[0];
                newTown2 = output[1];
            }

            //Add them back to the grid
            addToGrid(mapElitesGrid, newTown1);
            addToGrid(mapElitesGrid, newTown2);


        }

        return buildingifyGrid(mapElitesGrid);
        
    }

    //Generate random starting town in the form of a 2D int matrix
    private mapElitesTown generateRandomTown(){

        int[,] town = new int[townSize,townSize];

        //Generate the street vs building chance of this chunk
        int streetChance = Random.Range(1, 100);
        //Generate the max building height for this chunk
        int localMaxBuildHeight = Random.Range(1, maxBuildingHeight);

        //Random rnd = new Random();

        for (int x = 0; x<town.GetLength(0); x++){
            for (int z = 0; z<town.GetLength(1); z++){
                //Chance for each tile of being a street or a building
                if (Random.Range(1, 100)>streetChance){
                    town[x,z] = Random.Range(1, localMaxBuildHeight);
                }
                else{
                    town[x,z] = 0;
                }
            }
        }

        return new mapElitesTown((int[,]) town.Clone());
            
    }

    //Generate starting Map Elites grid
    private mapElitesTown[,] generateStartingGrid(){

        mapElitesTown[,] mapElitesGrid = new mapElitesTown[gridSize,gridSize];

        mapElitesTown[] startingPop = generateRandomPopulation(20);

        //Loop through each population
        for (int popLoc = 0; popLoc<startingPop.GetLength(0); popLoc++){
            //Loop through each cell in map elites map to see if it belongs in specified cell
            //If it does, check fitness of current entrant, replace it if its less fit
            mapElitesTown currTown = startingPop[popLoc];

            addToGrid(mapElitesGrid, currTown);
        }
        
        return mapElitesGrid;
    }

    //Generate array of randomised town chunks
    private mapElitesTown[] generateRandomPopulation(int popSize){

        mapElitesTown[] returnPop = new mapElitesTown[popSize];

        for (int i = 0; i < returnPop.GetLength(0); i++){
            returnPop[i] = generateRandomTown();
        }

        return returnPop;

    }

    //Add a level to a map elites grid
    private void addToGrid(mapElitesTown[,] mapElitesGrid, mapElitesTown townToAdd){

        for (int x = 0; x<mapElitesGrid.GetLength(0); x++){
            for (int y = 0; y<mapElitesGrid.GetLength(1); y++){
                if(checkBelongs(townToAdd, x, y, currRunType)){
                    if (mapElitesGrid[x,y] == null || mapElitesGrid[x,y].getFitness() < townToAdd.getFitness()){
                        mapElitesGrid[x,y] = townToAdd;
                        Debug.Log("Town added to map elites grid at location: " + x + "," + y);
                    }
                }
            }
        }        
    }

    //Method for checking whether a given level belongs in a given cell of a map elites grid, dependent on the run type (which behavioral features were looking at)
    private bool checkBelongs(mapElitesTown townToCheck, int xLoc, int yLoc, RUNTYPE currRunType){

        float streetCountRange = maxStreetTiles - minStreeTiles;
        float totalHeightRange = maxTotalHeight - minTotalHeight;
        float maxHeightRange = maxBuildingHeight;

        if (currRunType == RUNTYPE.THvSC){
            float townSC = townToCheck.getStreetCount();
            float townTH = townToCheck.getTotalHeight();

            float localSCmin = ((streetCountRange/gridSize)*xLoc) + minStreeTiles;
            float localSCmax = ((streetCountRange/gridSize)*(xLoc+1)) + minStreeTiles;

            float localTHmin = ((totalHeightRange/gridSize)*yLoc) + minTotalHeight;
            float localTHmax = ((totalHeightRange/gridSize)*(yLoc+1)) + minTotalHeight;

            //Debug.Log("Local SC range: " + localSCmin + "," + localSCmax + " and town SC: " + townSC);
            //Debug.Log("Local TH range: " + localTHmin + "," + localTHmax + " and town th: " + townTH);

            if(townSC>localSCmin&&townSC < localSCmax && townTH > localTHmin && townTH < localTHmax){
                //Debug.Log("Local SC range: " + localSCmin + "," + localSCmax + " and town SC: " + townSC);
                //Debug.Log("Local TH range: " + localTHmin + "," + localTHmax + " and town th: " + townTH);
                return true;
            }
            else{
                return false;
            }
        }
        else if (currRunType == RUNTYPE.THvMH){
            float townMH = townToCheck.getMaxHeight();
            float townTH = townToCheck.getTotalHeight();

            float localMHmin = ((maxHeightRange/gridSize)*xLoc);
            float localMHmax = ((maxHeightRange/gridSize)*(xLoc+1));

            float localTHmin = ((totalHeightRange/gridSize)*yLoc) + minTotalHeight;
            float localTHmax = ((totalHeightRange/gridSize)*(yLoc+1)) + minTotalHeight;

            if(townMH>localMHmin&&townMH < localMHmax && townTH > localTHmin && townTH < localTHmax){
                return true;
            }
            else{
                return false;
            }
        }
        else{
            return false;
        }
    }

    //Grab us a random level from a map elites grid
    private mapElitesTown getRandomLevelFromGrid(mapElitesTown[,] inputMap){

        bool selected =false ;

        mapElitesTown returnLevel = null;

        int checkCount = 0;
        int checkLimit = 10000;
        //Added the check count so it doesnt loop forever and crash when i do something wrong 
        while (!selected&&checkCount < checkLimit){
            int randx = Random.Range(0, gridSize);
            int randy = Random.Range(0, gridSize);

            if (inputMap[randx, randy] != null){
                returnLevel = inputMap[randx, randy].Clone();
                selected = true;
            }

            checkCount+=1;
        }

        if (checkCount == checkLimit){
            Debug.Log("Failed to find a random level in grid. Returning null");
        }

        return returnLevel;


    }

    //Basic mutation method. Takes in a town and has a chance of increasing or decreasing the height of each cell
    private mapElitesTown tileMutate(mapElitesTown input){
        int[,] townRep = (int[,]) input.getRepresentation().Clone();

        for (int x = 0; x<townRep.GetLength(0); x++){
            for (int y = 0; y<townRep.GetLength(1); y++){

                if (Random.Range(0, 1) < tileMutateChance){
                    
                    //50 50 chance of it getting higher or lower
                     if (Random.Range(0,10)<5){
                         if (townRep[x,y]>0){
                            //Debug.Log("Reducing height");
                            townRep[x,y] -=1;
                         }
                     }
                     else {
                        if (townRep[x,y]<maxBuildingHeight){
                            //Debug.Log("Increasing height");
                            townRep[x,y] +=1;
                        }
                     }
                }
            }
        }
        return new mapElitesTown(townRep);
    }

    //Mutation method, chance of duplicating the value of a neighbour cell into current cell, to promote building generation
    private mapElitesTown tileDuplicate(mapElitesTown input){
        int[,] townRep = (int[,]) input.getRepresentation().Clone();

        for (int x = 0; x<townRep.GetLength(0); x++){
            for (int y = 0; y<townRep.GetLength(1); y++){

                if (Random.Range(0, 1) < tileDuplicateChance){
                    
                    //Decided which direction we copy from
                    float directionSelector = Random.Range(0,100);

                    //Check directions are free
                    bool northFree = ((y+1)<townRep.GetLength(1));
                    bool southFree = ((y-1)>=0);
                    bool westFree = ((x-1)>=0);
                    bool eastFree = ((x+1)<townRep.GetLength(0));

                     if ((directionSelector<25)&&northFree){;
                        townRep[x,y] = townRep[x,y+1];
                     }
                     else if((directionSelector<50)&&southFree){
                        townRep[x,y] = townRep[x,y-1];
                     }
                     else if ((directionSelector<75)&&westFree){
                        townRep[x,y] = townRep[x-1,y];
                     }
                     else if (eastFree){
                         townRep[x,y]=townRep[x+1,y];
                     }

                }
            }
        }
        return new mapElitesTown(townRep);
    }

    private mapElitesTown[] crossover(mapElitesTown chunk1, mapElitesTown chunk2){
        int[,] chunkRep1 = (int[,]) chunk1.getRepresentation().Clone();
        int[,] chunkRep2 = (int[,]) chunk2.getRepresentation().Clone();

        //Bool for whether we are crossing over north - south or west - east
        bool northSouthSplit = (Random.Range(1,100)>50);
        //Pick random split point
        int splitPoint = Random.Range(0, gridSize);

        for (int x = 0; x<townSize; x++){
            for (int y = 0; y<townSize; y++){
                int chunk1Val = chunkRep1[x,y];
                int chunk2Val = chunkRep2[x,y];

                //Swap the blocks if we're past the crossover point
                if ((northSouthSplit&&x>splitPoint)||(!northSouthSplit&&y>splitPoint)){
                    chunkRep1[x,y] = chunk2Val;
                    chunkRep2[x,y] = chunk1Val;
                }

            }
        }
        mapElitesTown newTown1 = new mapElitesTown(chunkRep1);
        mapElitesTown newTown2 = new mapElitesTown(chunkRep2);

        return new mapElitesTown[]{newTown1,newTown2};

    }

    private int getPopulatedCellCount(mapElitesTown[,] inputGrid){
        int count = 0;

        for (int x = 0; x<inputGrid.GetLength(0); x++){
            for (int y = 0; y<inputGrid.GetLength(1); y++){
                if(inputGrid[x,y]!= null){
                    count+=1;
                }
            }
        }

        return count;
    }

    private int getTotalGridFitness(mapElitesTown[,] inputGrid){
        int total = 0;

        for (int x = 0; x<inputGrid.GetLength(0); x++){
            for (int y = 0; y<inputGrid.GetLength(1); y++){
                if(inputGrid[x,y]!= null){
                    total+=inputGrid[x,y].getFitness();
                }
            }
        }

        return total;
    }

    //Algorithm for turning a messy 2D int matrix into more distinct buildings with streets in between each
    private mapElitesTown ConvertToBuildings(int[,] townRep){

        int[,] outputRep = (int[,])townRep.Clone();

        ArrayList buildingList = new ArrayList();

        //Loop through all building heights 
        for (int i = maxBuildingHeight; i>0 ; i--){

            //Loop through all cells, looking for cells with that height
            for (int x = 0; x<outputRep.GetLength(0); x++){
                for (int y = 0; y<outputRep.GetLength(1); y++){
                    //Logic for if we have found a cell with the current height
                    if(outputRep[x,y]== i&&outputRep[x,y]!=0){
                        //Store north west corner of building triangle
                        int[] nwC = new int[] {x,y};
                        //Create storage for south east corner
                        int[] seC = new int[] {x,y};

                        bool stillInBuildingy = true;
                        bool stillInBuildingx = true;
                        //Find limits of the building
                        for (int xl = x; xl<outputRep.GetLength(0); xl++){
                            for (int yl = y; yl<outputRep.GetLength(1); yl++){
                                if((outputRep[xl,yl]== i)&&stillInBuildingy&&stillInBuildingx){
                                    if (xl>seC[0]){
                                        seC[0] = xl;
                                    }
                                    seC[0] = xl;
                                    seC[1] = yl;
                                }
                                else{
                                    stillInBuildingy = false;
                                    stillInBuildingx = false;
                                }
                            }
                            stillInBuildingy = true;
                        }

                        //Create building and add it to list
                        buildingList.Add(new building(nwC, seC, i));

                        //Set everything around to street level
                        //First check which edges we are next to
                        bool northFree = ((nwC[0])>0);
                        bool southFree = (seC[0]<outputRep.GetLength(0)-1);
                        bool westFree = ((nwC[1])>0);
                        bool eastFree = (seC[1]<outputRep.GetLength(1)-1);
                        //Debug.Log("Building found. Height: " + i +". NW Corner: " + nwC[0]+","+nwC[1] + ". SE Corner: " + seC[0]+","+seC[1]+
                        //    "NorthFree: " + northFree + ". SouthFree: " + southFree +"Westfree: "+ westFree + ". Eastfree: " + eastFree);
                        //Loop through surrounding tiles
                        if (northFree){
                            for (int n = nwC[1]; n < seC[1]+1; n ++){
                                //Debug.Log("North Leveling: " + (nwC[0]-1) +","+n + "for height" + i + "& building " + nwC[0]+","+nwC[1] + ". " + seC[0]+","+seC[1]);
                                outputRep[(nwC[0]-1),n]=0;
                            }
                        }
                        if (southFree){
                            for (int n = nwC[1]; n < seC[1]+1; n ++){
                                //Debug.Log("South Leveling: " + (seC[0]+1) +","+n + "for height" + i + "& building " + nwC[0]+","+nwC[1] + ". " + seC[0]+","+seC[1]);
                                outputRep[(seC[0]+1), n]=0;
                            }
                        }
                        if (westFree){
                            for (int n = nwC[0]; n < seC[0]+1; n ++){
                                //Debug.Log("West Leveling: " + n +","+(nwC[1]-1) + "for height" + i + "& building " + nwC[0]+","+nwC[1] + ". " + seC[0]+","+seC[1]);
                                outputRep[n, (nwC[1]-1)]=0;
                            }
                        }
                        if (eastFree){
                            for (int n = nwC[0]; n < seC[0]+1; n ++){
                                //Debug.Log("East Leveling: " + n +","+(seC[1]+1) + "for height" + i + "& building " + nwC[0]+","+nwC[1] + ". " + seC[0]+","+seC[1]);                                
                                outputRep[n, (seC[1]+1)]=0;
                            }
                        }
                        if(northFree&&westFree){
                            outputRep[(nwC[0]-1),(nwC[1]-1)]=0;
                        }
                        if(northFree&&eastFree){
                            outputRep[(nwC[0]-1),(seC[1]+1)]=0;
                        }
                        if(southFree&&westFree){
                            outputRep[(seC[0]+1),(nwC[1]-1)]=0;
                        }
                        if(southFree&&eastFree){
                            outputRep[(seC[0]+1),(seC[1]+1)]=0;
                        }

                        //Skip the rest of this height that we've covered
                        x = seC[0];
                        y = seC[1];

                    }
                }
            }

        }

        return new mapElitesTown(outputRep, buildingList);

    }

    
    private mapElitesTown[,] buildingifyGrid(mapElitesTown[,] inputGrid){

        mapElitesTown[,] outputGrid = new mapElitesTown[gridSize,gridSize];       
        
        for (int x = 0; x<inputGrid.GetLength(0); x++){
            for (int y = 0; y<inputGrid.GetLength(1); y++){
                if(inputGrid[x,y]!= null){
                    outputGrid[x,y]=ConvertToBuildings(inputGrid[x,y].getRepresentation());
                }
            }
        }

        return outputGrid;
    }

}
