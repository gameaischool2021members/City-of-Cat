using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class building
{
    private int[] northWestCorner = new int[2];
    private int[] southEastCorner = new int[2];
    private int height;
    private int[] xyOffset = new int[2];

    public building(int[] NWCorner, int[] SECorner, int height){
        this.northWestCorner = NWCorner;
        this.southEastCorner = SECorner;
        this.height = height;
    }

    public void printInfo(){
        Debug.Log("Buidling Abs Corners: " + getAbsNWCorner()[0] + "," + getAbsNWCorner()[1] + "." +  getAbsNECorner()[0] + "," + getAbsNECorner()[1] + "." +
                getAbsSWCorner()[0] + "," + getAbsSWCorner()[1] + "." + getAbsSECorner()[0] + "," + getAbsSECorner()[1] + "." +
                "Buidling Local Corners: " + northWestCorner[0] + "," + northWestCorner[1] + "." +  southEastCorner[0] + "," + southEastCorner[1] + "." +
                        " Building height: " + height);
    }

    public int getHeight(){
        return height;
    }

    //These methods to get the global corner positions of the building
    public int[] getAbsNWCorner(){
        return  new int[]{northWestCorner[0]+xyOffset[0], northWestCorner[1]+xyOffset[1]};
    }
    public int[] getAbsNECorner(){
        return  new int[]{northWestCorner[0]+xyOffset[0], southEastCorner[1]+xyOffset[1]};
    }

    public int[] getAbsSWCorner(){
        return  new int[]{southEastCorner[0]+xyOffset[0], northWestCorner[1]+xyOffset[1]};
    }

    public int[] getAbsSECorner(){
        return  new int[]{southEastCorner[0]+xyOffset[0], southEastCorner[1]+xyOffset[1]};
    }

    public int[] getNWCorner(){
        return northWestCorner;
    }

    public int[] getSECorner(){
        return southEastCorner;
    }

    public void setxyOffset(int[] offset){
        xyOffset = offset;

        //printInfo();
    }

    public int[] getxyOffset(){
        return xyOffset;
    }
}
