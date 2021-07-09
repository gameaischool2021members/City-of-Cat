using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class WFC
{
    public class Vocabulary {
        public string Code { get; set; }
        public string Name { get; set; }
        public Dictionary<string, List<string>> Allowed = new Dictionary<string, List<string>>();

        List<string> dimensions = new List<string>{"x", "y", "z"};
        List<string> directions = new List<string>{"p", "n"};

        public Vocabulary (string code, string name){
            Code = code;
            Name = name;
            foreach(var dim in dimensions) {
                foreach(var dir in directions) {
                    Allowed.Add( dim+dir, new List<string>() );
                }
            }
        }
    }

    // TODO: move this outside of this class and improve extendibility
    private Vocabulary G;
    private Vocabulary N;
    private Vocabulary X;
    private Vocabulary T;
    private Vocabulary A1;
    private Vocabulary A2;
    private Vocabulary A3;
    private Vocabulary A4;
    private Vocabulary B1;
    private Vocabulary B3;
    private Vocabulary B4;
    private Vocabulary C1;
    private Vocabulary C2;
    private Vocabulary C3;
    private Vocabulary C4;
    private Dictionary<string, Vocabulary> dictionary;
    private Dictionary<(int,int,int), List<string>> site;
    private List<(int,int,int)> coordCollapseable;
    private List<(int,int,int)> coordVisitable;
    private int maxH;
    private int planX;
    private int planY;
    private int planXPadded;
    private int planYPadded;
    private int planZPadded;
    static System.Random rnd;

    /*
    public WFC (int h, int x, int y) {
        Initialize(h, x, y);
        Debug.Log("heyyyy");
    }
    */

    public void RunWFC() {
        int iteration = 0;
        int attempt = 0;
        int maxAttempt = 200;

        while (!_IsCollapsedAll()) {
            //(int,int,int) collapsed_coord = _CollapseLowestEntropy();
            (int,int,int) collapsed_coord = _CollapseViaHeuristics();
            bool res = _PropagateFrom(collapsed_coord);
            iteration ++;

            if (!res) {
                _ResetSite(maxH, planX, planY);
                iteration = 0;
                Debug.Log("RunWFC(): reset.");

                if (attempt > maxAttempt) {
                    Debug.Log("RunWFC(): attempt > the predefined stopping number (" + maxAttempt + "), quiting exploration now.");
                    return;
                }
                continue;
            }
        }
        Debug.Log("RunWFC(): success.");
    }

    public void PrintVisitableSpace() {
        Debug.Log("PrintVisitableSpace():");
        foreach (var coord in coordVisitable) {
            Debug.Log(coord + "," + site[coord][0]);
        }
    }

    public Dictionary<(int,int,int), string> GetFinalStructure() {
        Dictionary<(int,int,int), string> final = new Dictionary<(int,int,int), string>();
        //foreach ((int,int,int) c in site.Keys) {
        foreach ((int,int,int) c in coordVisitable) {
            try {
                final.Add( c, site[c][0] );
            }
            catch (Exception e) {
                Debug.Log("Exception has occurred: " + e);
                Debug.Log("Exception occurred when c is " + c.ToString());
            }  
            
        }
        return final;
    }

    //////////////////////////
    // Helper functions
    //////////////////////////

    void _ResetSite(int h, int x, int y) {
        site = new Dictionary<(int,int,int), List<string>>();
        coordCollapseable = new List<(int,int,int)>();
        coordVisitable = new List<(int,int,int)>();
        maxH = h;
        planX = x;
        planY = y;
        planXPadded = planX+2;
        planYPadded = planY+2;
        planZPadded = maxH+2;
        
        for (int i = 0; i < planXPadded; i++) {
            for (int j = 0; j < planYPadded; j++) {
                for (int k = 0; k < planZPadded; k++) {
                    if ( (i==0) || (i==planXPadded-1) || (j==0) || (j==planYPadded-1) ) {
                        site.Add( (i,j,k), new List<string>() {"X"} );
                    }
                    else if (k==0) {
                        site.Add( (i,j,k), new List<string>() {"G"} );
                        coordVisitable.Add( (i,j,k) );
                    }
                    else if (k==planZPadded-1) {
                        site.Add( (i,j,k), new List<string>() {"T"} );
                    }
                    else {
                        site.Add( (i,j,k), new List<string>() {"A1","A2","A3","A4","B1","B3","B4","C1","C2","C3","C4","N"} );
                        coordVisitable.Add( (i,j,k) );
                        coordCollapseable.Add( (i,j,k) );
                    }
                }
            }
        }

        _PropagateFrom( (1,1,0) );
    }

    private bool _IsCollapsedAll() {
        bool res = true;
        foreach (List<string> states in site.Values){
            if (states.Count > 1) {
                res = false;
                break;
            }
        }
        return res;
    }

    private List<(int,int,int)> _GetAllGndCoords() {
        List<(int,int,int)> coords = new List<(int,int,int)>();
        foreach ((int,int,int) c in site.Keys) {
            if ( site[c].SequenceEqual(new List<string>() {"G"}) ) {
                coords.Add(c);
            }
        }
        return coords;
    }

    private List<(int,int,int)> _GetAllTopCoords() {
        List<(int,int,int)> coords = new List<(int,int,int)>();
        foreach ((int,int,int) c in site.Keys) {
            if ( site[c].SequenceEqual(new List<string>() {"T"}) ) {
                coords.Add(c);
            }
        }
        return coords;
    }
    
    private (int,int,int) _CollapseViaHeuristics() {
        // Find a lowest (z-value) collapseable coord, then
        // collapse it with a probability distribution depending on z-value
        // such that when z-value is large relatively to Z, roof-units have higher probability of being chosen.
        
        int min_z = planZPadded*2; // arbitrarily large number
        foreach(var coord in coordCollapseable) {
            if ( (coord.Item3 < min_z) && (site[coord].Count > 1) ) {
                min_z = coord.Item3;
            }
        }
        List<(int,int,int)> candidates = new List<(int,int,int)>();
        foreach(var coord in coordCollapseable) { 
            if ( (coord.Item3 == min_z) && (site[coord].Count > 1) ) {
                candidates.Add(coord);
            }
        }

        // pick a coord to collapse
        int index = rnd.Next(candidates.Count);
        (int,int,int) chosen_coord = candidates[index];

        // collapse to a random state at that coord
        index = rnd.Next(site[chosen_coord].Count);
        site[chosen_coord] = new List<string>() {site[chosen_coord][index]};

        return chosen_coord;
    }

    // Find the coord of lowest-entropy to collapse; warning: this can result in hanging when the site space is nontrivially large e.g. (x,y,z) = (3,3,5)
    private (int,int,int) _CollapseLowestEntropy() {
        int min_len = 10000;
        foreach(var coord in coordCollapseable) { 
            if ( (site[coord].Count < min_len) && (site[coord].Count > 1) ) {
                min_len = site[coord].Count;
            }
        }

        List<(int,int,int)> candidates = new List<(int,int,int)>();
        foreach(var coord in coordCollapseable) { 
            if ( (site[coord].Count == min_len) && (site[coord].Count > 1) ) {
                candidates.Add(coord);
            }
        }

        // pick a coord to collapse
        int index = rnd.Next(candidates.Count);
        (int,int,int) chosen_coord = candidates[index];

        // collapse to a random state at that coord
        index = rnd.Next(site[chosen_coord].Count);
        site[chosen_coord] = new List<string>() {site[chosen_coord][index]};

        return chosen_coord;
    }

    private bool _IsCoordLegal( (int,int,int) coord ) {
        if ( (1 <= coord.Item1) && (coord.Item1 < planXPadded-1) &&
             (1 <= coord.Item2) && (coord.Item2 < planYPadded-1) &&
             (1 <= coord.Item3) && (coord.Item3 < planZPadded-1) ) {
            return true;
        }
        else {
            return false;
        }
    }

    private int _CountOccurence( List<int> l, int n ) {
        int count = 0;
        foreach (int e in l) {
            if (e == n) {
                count++;
            }
        }
        return count;
    }

    private bool _IsCoordAdjacent( (int,int,int) coord1, (int,int,int) coord2 ) {
        int dx_abs = Math.Abs(coord1.Item1 - coord2.Item1);
        int dy_abs = Math.Abs(coord1.Item2 - coord2.Item2);
        int dz_abs = Math.Abs(coord1.Item3 - coord2.Item3);
        if ( (dx_abs>1) || (dy_abs>1) || (dz_abs>1) ) {
            return false;
        }
        else if ( _CountOccurence(new List<int>() {dx_abs, dy_abs, dz_abs}, 0) != 2 ) {
            return false;
        }
        else {
            return true;
        }
    }

    private bool _CollapseViaRule( (int,int,int) coordTarget, (int,int,int) coordImpact ) {
        int dx = coordTarget.Item1 - coordImpact.Item1;
        int dy = coordTarget.Item2 - coordImpact.Item2;
        int dz = coordTarget.Item3 - coordImpact.Item3;
        string direction;
        if (dx!=0) {
            if (dx>0) {
                direction = "xp";
            }
            else {
                direction = "xn";
            }
        }
        else if (dy!=0) {
            if (dy>0) {
                direction = "yp";
            }
            else {
                direction = "yn";
            }
        }
        else {
            if (dz>0) {
                direction = "zp";
            }
            else {
                direction = "zn";
            }
        }

        List<string> statesAfterCollapse = new List<string>();

        foreach (string voc_code_impact in site[coordImpact]) {
            //Vocabulary voc_impact = dictionary[voc_code_impact];
            List<string> allowed = dictionary[voc_code_impact].Allowed[direction];

            List<string> intersect = site[coordTarget].Intersect(allowed).ToList();
            intersect.Sort();
            statesAfterCollapse = statesAfterCollapse.Concat(intersect).ToList();
        }
        statesAfterCollapse = statesAfterCollapse.Distinct().ToList();
        statesAfterCollapse.Sort();
        site[coordTarget] = statesAfterCollapse;

        if (site[coordTarget].Count==0) {
            return false;
        }
        else {
            return true;
        }

    }

    // Propagate the information from the recently collapsed coord to the rest;
    // break and return False if any coord collapses to empty list i.e. illegal configuration
    private bool _PropagateFrom( (int,int,int) source_coord ) {
        int radius = 1;
        List<(int,int,int)> source_coord_list = new List<(int,int,int)>() {source_coord};
        List<(int,int,int)> visited = source_coord_list.Concat( _GetAllGndCoords() ).ToList();
        visited = visited.Concat( _GetAllTopCoords() ).ToList();
        visited = visited.Distinct().ToList();

        int test = Math.Max(2,3);
        while ( radius <= Math.Max(planXPadded, Math.Max(planYPadded,planZPadded)) ) {
            for (int i=-1*radius; i<=radius; i++) {
                for (int j=-1*radius; j<=radius; j++) {
                    for (int k=-1*radius; k<=radius; k++) {
                        (int,int,int) coord = (i,j,k);
                        if ( _IsCoordLegal(coord) && (!visited.Contains(coord)) ) {
                            List<(int,int,int)> impact_coords = new List<(int,int,int)>();
                            
                            foreach (var c in visited) {
                                if ( _IsCoordAdjacent(coord, c) ) {
                                    impact_coords.Add(c);
                                }
                            }

                            foreach (var c in impact_coords) {
                                bool success = _CollapseViaRule(coord, c);
                                if (!success) {
                                    return false;
                                }
                            }
                            visited.Add(coord);
                        }
                    }
                }
            }

            radius += 1;
        }

        return true;
    }

    // Initialize the dictionary and the adjacency rules among structural pieces. TODO: improve extendibility by providing a interface for adding rules
    public void Initialize(int h, int x, int y)
    {
        rnd = new System.Random();

        G  = new Vocabulary ("G",  "Ground");
        N  = new Vocabulary ("N",  "Empty");
        X  = new Vocabulary ("X",  "Padding-side-forbidden");
        T  = new Vocabulary ("T",  "Padding-top-forbidden");

        A1 = new Vocabulary ("A1", "Ground-room-door-xp");
        A2 = new Vocabulary ("A2", "Ground-room-door-yp");
        A3 = new Vocabulary ("A3", "Ground-2room-xp");
        A4 = new Vocabulary ("A4", "Ground-2room-xn");

        B1 = new Vocabulary ("B1", "Mid-room");
        B3 = new Vocabulary ("B3", "Mid-2room-xp");
        B4 = new Vocabulary ("B4", "Mid-2room-xn");

        C1 = new Vocabulary ("C1", "Roof-room-yp");
        C2 = new Vocabulary ("C2", "Roof-room-xp");
        C3 = new Vocabulary ("C3", "Roof-2room-xp");
        C4 = new Vocabulary ("C4", "Roof-2room-xn");

        dictionary = new Dictionary<string, Vocabulary>();
        dictionary.Add( "G",  G );
        dictionary.Add( "N",  N );
        dictionary.Add( "X",  X );
        dictionary.Add( "T",  T );
        dictionary.Add( "A1", A1 );
        dictionary.Add( "A2", A2 );
        dictionary.Add( "A3", A3 );
        dictionary.Add( "A4", A4 );
        dictionary.Add( "B1", B1 );
        dictionary.Add( "B3", B3 );
        dictionary.Add( "B4", B4 );
        dictionary.Add( "C1", C1 );
        dictionary.Add( "C2", C2 );
        dictionary.Add( "C3", C3 );
        dictionary.Add( "C4", C4 );


        G.Allowed["zp"] = new List<string>() {"A1", "A2", "A3", "A4"};
        G.Allowed["xp"] = G.Allowed["xn"] = G.Allowed["yp"] = G.Allowed["yn"] = new List<string>() {"G"};

        N.Allowed["zp"] = new List<string>() {"N"};
        N.Allowed["zn"] = new List<string>() {"C1", "C2", "C3", "C4", "N"};
        N.Allowed["xp"] = N.Allowed["xn"] = N.Allowed["yp"] = N.Allowed["yn"] = new List<string>() {"A1","A2","A3","A4","B1","B3","B4","C1","C2","C3","C4","N","X"};

        X.Allowed["zp"] = X.Allowed["zn"] = new List<string>() {"X"};
        X.Allowed["xp"] = X.Allowed["xn"] = X.Allowed["yp"] = X.Allowed["yn"] = new List<string>() {"A1","A2","A3","A4","B1","B3","B4","C1","C2","C3","C4","N","X"};

        T.Allowed["zn"] = new List<string>() {"C1", "C2", "C3", "C4", "N"};
        T.Allowed["xp"] = T.Allowed["xn"] = T.Allowed["yp"] = T.Allowed["yn"] = new List<string>() {"T"};

        A1.Allowed["zp"] = new List<string>() {"B1","C1","C2"};
        A1.Allowed["zn"] = new List<string>() {"G"};
        A1.Allowed["xp"] = new List<string>() {"A1", "A2", "A4", "X"};
        A1.Allowed["xn"] = new List<string>() {"A1", "A2", "A3", "X"};
        A1.Allowed["yp"] = A1.Allowed["yn"] = new List<string>() {"A1", "A2", "A3", "A4", "X"};

        A2.Allowed["zp"] = new List<string>() {"B1","C1","C2"};
        A2.Allowed["zn"] = new List<string>() {"G"};
        A2.Allowed["xp"] = new List<string>() {"A1", "A2", "A4", "X"};
        A2.Allowed["xn"] = new List<string>() {"A1", "A2", "A3", "X"};
        A2.Allowed["yp"] = A2.Allowed["yn"] = new List<string>() {"A1", "A2", "A3", "A4", "X"};

        A3.Allowed["zp"] = new List<string>() {"B3", "C3"};
        A3.Allowed["zn"] = new List<string>() {"G"};
        A3.Allowed["xp"] = new List<string>() {"A1", "A2", "A4", "X"};
        A3.Allowed["xn"] = new List<string>() {"A4"};
        A3.Allowed["yp"] = A3.Allowed["yn"] = new List<string>() {"A1", "A2", "A3", "A4", "X"};

        A4.Allowed["zp"] = new List<string>() {"B4", "C4"};
        A4.Allowed["zn"] = new List<string>() {"G"};
        A4.Allowed["xp"] = new List<string>() {"A3"};
        A4.Allowed["xn"] = new List<string>() {"A1", "A2", "A3", "X"};
        A4.Allowed["yp"] = A4.Allowed["yn"] = new List<string>() {"A1", "A2", "A3", "A4", "X"};

        B1.Allowed["zn"] = new List<string>() {"A1", "A2", "B1"};
        B1.Allowed["zp"] = new List<string>() {"B1", "C1", "C2"};
        B1.Allowed["xp"] = new List<string>() {"B1", "B4", "C1", "C2", "C4", "N", "X"};
        B1.Allowed["xn"] = new List<string>() {"B1", "B3", "C1", "C2", "C3", "N", "X"};
        B1.Allowed["yp"] = B1.Allowed["yn"] = new List<string>() {"B1", "B3", "B4", "C1", "C2", "C3", "C4", "N", "X"};

        B3.Allowed["zn"] = new List<string>() {"A3"};
        B3.Allowed["zp"] = new List<string>() {"B3", "C3"};
        B3.Allowed["xn"] = new List<string>() {"B4"};
        B3.Allowed["xp"] = new List<string>() {"B1", "B4", "C1", "C2", "C4", "N", "X"};
        B3.Allowed["yn"] = B3.Allowed["yp"] = new List<string>() {"B1", "B3", "B4", "C1", "C2", "C3", "C4", "N", "X"};

        B4.Allowed["zn"] = new List<string>() {"A4"};
        B4.Allowed["zp"] = new List<string>() {"B4", "C4"};
        B4.Allowed["xn"] = new List<string>() {"B1", "B3", "C1", "C2", "C3", "N", "X"};
        B4.Allowed["xp"] = new List<string>() {"B3"};
        B4.Allowed["yn"] = B4.Allowed["yp"] = new List<string>() {"B1", "B3", "B4", "C1", "C2", "C3", "C4", "N", "X"};

        C1.Allowed["zn"] = new List<string>() {"A1", "A2", "B1"};
        C1.Allowed["zp"] = new List<string>() {"N"};
        C1.Allowed["xn"] = new List<string>() {"B1", "B3", "C1", "C2", "C3", "N", "X"};
        C1.Allowed["xp"] = new List<string>() {"B1", "B4", "C1", "C2", "C4", "N", "X"};
        C1.Allowed["yn"] = C1.Allowed["yp"] = new List<string>() {"B1", "B3", "B4", "C1", "C2", "C3", "C4", "N", "X"};

        C2.Allowed["zn"] = new List<string>() {"A1", "A2", "B1"};
        C2.Allowed["zp"] = new List<string>() {"N"};
        C2.Allowed["xn"] = new List<string>() {"B1", "B3", "C1", "C2", "C3", "N", "X"};
        C2.Allowed["xp"] = new List<string>() {"B1", "B4", "C1", "C2", "C4", "N", "X"};
        C2.Allowed["yn"] = C2.Allowed["yp"] = new List<string>() {"B1", "B3", "B4", "C1", "C2", "C3", "C4", "N", "X"};

        C3.Allowed["zn"] = new List<string>() {"B3"};
        C3.Allowed["zp"] = new List<string>() {"N"};
        C3.Allowed["xn"] = new List<string>() {"C4"};
        C3.Allowed["xp"] = new List<string>() {"B1", "B4", "C1", "C2", "C4", "N", "X"};
        C3.Allowed["yn"] = C3.Allowed["yp"] = new List<string>() {"B1", "B3", "B4", "C1", "C2", "C3", "C4", "N", "X"};

        C4.Allowed["zn"] = new List<string>() {"B4"};
        C4.Allowed["zp"] = new List<string>() {"N"};
        C4.Allowed["xn"] = new List<string>() {"B1", "B3", "C1", "C2", "C3", "N", "X"};
        C4.Allowed["xp"] = new List<string>() {"C3"};
        C4.Allowed["yn"] = C4.Allowed["yp"] = new List<string>() {"B1", "B3", "B4", "C1", "C2", "C3", "C4", "N", "X"};

        _ResetSite(h, x, y);

        Debug.Log("Initialization complete.");
    }
}
