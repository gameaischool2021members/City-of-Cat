import numpy as np
import random
import pprint

## Helper class / function
class Vocabulary:
    def __init__(self, code="", name=""):
        self.code = code
        self.name = name
        self.allowed = {}
        for dimension in ['x', 'y', 'z']:
            for direction in ['p', 'n']:
                self.allowed[dimension+direction] = []

## Dictionary
G  = Vocabulary(code='G',  name='GND')
N  = Vocabulary(code='N',  name='EMPTY')
X  = Vocabulary(code='X',  name='PADDING-SIDE-FORBIDDEN')
T  = Vocabulary(code='T',  name='PADDING-TOP-FORBIDDEN')
A1 = Vocabulary(code='A1', name='GND-COLUMN')
A2 = Vocabulary(code='A2', name='GND-1ROOM')
B1 = Vocabulary(code='B1', name='MID-COLUMN')
B2 = Vocabulary(code='B2', name='MID-1ROOM')
B3 = Vocabulary(code='B3', name='MID-2ROOMX-1')
B4 = Vocabulary(code='B4', name='MID-2ROOMX-2')
C1 = Vocabulary(code='C1', name='ROOF-1ROOM')
C2 = Vocabulary(code='C2', name='ROOF-2ROOMX-1')
C3 = Vocabulary(code='C3', name='ROOF-2ROOMX-2')
voc_s = [G, N, X, T,
         A1, A2,
         B1, B2, B3, B4,
         C1, C2, C3]

gnd_units  = ['A1', 'A2']
mid_units  = ['B1', 'B2', 'B3', 'B4']
roof_units = ['C1', 'C2', 'C3']

## Rules
# ground
G.allowed['zp'] = gnd_units+['N']
G.allowed['zn'] = []
G.allowed['xp'] = G.allowed['xn'] = G.allowed['yp'] = G.allowed['yn'] = ['G']

# empty
N.allowed['zp'] = ['N']
N.allowed['zn'] = roof_units + ['G','N']
N.allowed['xp'] = N.allowed['xn'] = N.allowed['yp'] = N.allowed['yn'] = gnd_units + mid_units + roof_units + ['N','X']

# padding-side (forbidden)
X.allowed['zp'] = X.allowed['zn'] = ['X']
X.allowed['xp'] = X.allowed['xn'] = X.allowed['yp'] = X.allowed['yn'] = gnd_units + mid_units + roof_units + ['N','X']

# padding-top (forbidden)
T.allowed['zp'] = []
T.allowed['zn'] = roof_units + ['N']
T.allowed['xp'] = T.allowed['xn'] = T.allowed['yp'] = T.allowed['yn'] =['T']

# ground-column
A1.allowed['zp'] = mid_units + roof_units
A1.allowed['zn'] = ['G']
A1.allowed['xp'] = A1.allowed['xn'] = A1.allowed['yp'] = A1.allowed['yn'] = gnd_units + ['N', 'X']

# ground-1room
A2.allowed['zp'] = mid_units + roof_units
A2.allowed['zn'] = ['G']
A2.allowed['xp'] = A2.allowed['xn'] = A2.allowed['yp'] = A2.allowed['yn'] = gnd_units + ['N', 'X']

# mid-column
B1.allowed['zn'] = ['A1', 'B1'] # columns only beneath
B1.allowed['zp'] = mid_units + roof_units
B1.allowed['xn'] = B1.allowed['xp'] = B1.allowed['yn'] = B1.allowed['yp'] = mid_units + roof_units + ['N', 'X']

# mid-1room
B2.allowed['zn'] = gnd_units + mid_units
B2.allowed['zp'] = ['B2', 'B3', 'B4', 'C1', 'C2', 'C3']
B2.allowed['xn'] = ['B2','B4'] + roof_units + ['N','X']
B2.allowed['xp'] = ['B2','B3'] + roof_units + ['N','X']
B2.allowed['yn'] = B2.allowed['yp'] = ['B2','B3','B4'] + roof_units + ['N','X']

# mid-2roomx-1
B3.allowed['zn'] = gnd_units + mid_units
B3.allowed['zp'] = ['B2', 'B3', 'B4'] + roof_units
B3.allowed['xn'] = ['B1', 'B2', 'B4'] + roof_units + ['N','X']
B3.allowed['xp'] = ['B4']
B3.allowed['yn'] = B3.allowed['yp'] = mid_units + roof_units + ['N','X']

# mid-2roomx-2
B4.allowed['zn'] = gnd_units + mid_units
B4.allowed['zp'] = ['B2', 'B3', 'B4'] + roof_units
B4.allowed['xn'] = ['B3']
B4.allowed['xp'] = ['B1', 'B2', 'B3'] + roof_units + ['N','X']
B4.allowed['yn'] = B4.allowed['yp'] = mid_units + roof_units + ['N','X']

# roof-1room
C1.allowed['zn'] = gnd_units + ['B1','B2']
C1.allowed['zp'] = ['N']
C1.allowed['xn'] = ['B1','B2','B4'] + roof_units + ['N','X']
C1.allowed['xp'] = ['B1','B2','B3'] + roof_units + ['N','X']
C1.allowed['yn'] = C1.allowed['yp'] = mid_units + roof_units + ['N','X']


# roof-2roomx-1
C2.allowed['zn'] = ['B3']
C2.allowed['zp'] = ['N']
C2.allowed['xn'] = ['B1','B2','B4'] + roof_units + ['N','X']
C2.allowed['xp'] = ['B1','B2','B3'] + roof_units + ['N','X']
C2.allowed['yn'] = C2.allowed['yp'] = mid_units + roof_units + ['N','X']

# roof-2roomx-2
C3.allowed['zn'] = ['B4']
C3.allowed['zp'] = ['N']
C3.allowed['xn'] = ['B1','B2','B4'] + roof_units + ['N','X']
C3.allowed['xp'] = ['B1','B2','B3'] + roof_units + ['N','X']
C3.allowed['yn'] = C3.allowed['yp'] = mid_units + roof_units + ['N','X']

## Reverse mapping from piece's name to corresponding class instance
dictionary = {}
for voc in voc_s:
    dictionary[voc.code] = voc

## WFC
class WFC:
    def __init__(self):
        self.site = {}

    def reset_site(self, max_h = 2, section = (10,10)):
        site = {}
        site_collapseable = []
        X = section[0]+2
        Y = section[1]+2
        Z = max_h+2
        visitable_coords = []
        for x in range(X):
            for y in range(Y):
                for z in range(Z):
                    if (x==0) or (x==X-1) or (y==0) or (y==Y-1):
                        site[(x,y,z)] = ['X']
                    elif z==0:
                        site[(x,y,z)] = ['G']
                        visitable_coords.append((x,y,z))
                    elif z==Z-1:
                        site[(x,y,z)] = ['T']
                    else:
                        site[(x,y,z)] = gnd_units + mid_units + roof_units + ['N']
                        site_collapseable.append((x,y,z))
                        visitable_coords.append((x,y,z))
        self.site = site
        self.site_collapseable = site_collapseable
        self.populatable_count = section[0] * section[1] * max_h
        self.visitable_coords = visitable_coords
        self.max_h = max_h
        self.section = section
        self.X = X
        self.Y = Y
        self.Z = Z
        #print(f'self.visitable_coords={self.visitable_coords}')
        self._propagate_from((1,1,0))

    def run_wfc(self): # wave function collapse algorithm
        iteration=0
        while(not self._is_collapsed_all()):
            collapsed_coord = self._collapse_next()
            #print(f'collapsed {collapsed_coord} into {self.site[collapsed_coord]}')
            res = self._propagate_from(collapsed_coord)
            iteration+=1
            if not res:
                self.reset_site(self.max_h, self.section)
                iteration=0
                print('reset.')
                continue
            assert iteration<=self.populatable_count, "wfc iteration > populatable count"
        print('success.')

    def print_visitable_space(self):
        for coord in self.visitable_coords:
            print(f'{coord}: {self.site[coord]}')

    ### helper functions ###
    def _is_collapsed_all(self): # check if all populatable coords have collapsed
        res = True
        for states in self.site.values():
            if len(states)>1:
                res = False
                break
        return res

    def _get_all_gnd_coords(self):
        gnd_coords = []
        for coord in self.site:
            if self.site[coord] == ['G']:
                gnd_coords.append(coord)
        return gnd_coords

    def _get_all_top_coords(self):
        top_coords = []
        for coord in self.site:
            if self.site[coord] == ['T']:
                top_coords.append(coord)
        return top_coords

    def _collapse_next(self): # find next coord to collapse (shortest states whose len>1) and collapse it
        min_len = min([len(self.site[coord]) for coord in self.site_collapseable])

        min_len = 100 # arbitrarily large number
        for coord in self.site_collapseable:
            if ( len(self.site[coord])<min_len ) and ( len(self.site[coord])>1 ):
                min_len = len(self.site[coord])

        candidates = []
        for coord in self.site_collapseable:
            if len(self.site[coord]) == min_len:
                candidates.append(coord)
        coord = random.choice(candidates)
        state = random.choice(self.site[coord])
        self.site[coord] = [state]

        return coord

    def _propagate_from(self, source_coord): # propagate the information from recently collapsed coord to the rest
        # note: break and return False if any coord collapses to empty list i.e. illegal
        radius = 1
        visited = list( set([source_coord]+self._get_all_gnd_coords()+self._get_all_top_coords()) )
        #print(f'being _propogate_from() with visited: {visited}')

        # propagate in cocentric sphere of progressively larger radius
        # TODO: need a different alg for irregular plan
        while(radius<=max(self.X,self.Y,self.Z)):
            for x in range(-1*radius, radius+1):
                for y in range(-1*radius, radius+1):
                    for z in range(-1*radius, radius+1):
                        coord = (x,y,z)
                        #print(f'\nexamining coord: {coord}')

                        if self._is_coord_legal(coord) and (coord not in visited) and (len(self.site[coord])>1):
                            impact_coords = []
                            for c in visited:
                                if self._is_coord_adjacent(coord,c):
                                    impact_coords.append(c)
                            #print(f'impact_coords: {impact_coords}')
                            for c in impact_coords:
                                success = self._collapse_via_rule(coord,c)
                                #print()
                                if not success:
                                    #print('propagation failed.\n')
                                    return False
                            visited.append(coord) # update cache
            radius += 1

        #print('propagation succeeded.\n')
        return True

    def _is_coord_legal(self, coord):
        if (1<=coord[0]<self.X-1) and (1<=coord[1]<self.Y-1) and (0<=coord[2]<self.Z-1):
            return True
        else:
            return False

    def _is_coord_adjacent(self,coord1,coord2):
        dx_abs = abs(coord1[0]-coord2[0])
        dy_abs = abs(coord1[1]-coord2[1])
        dz_abs = abs(coord1[2]-coord2[2])
        if (dx_abs>1) or (dy_abs>1) or (dz_abs>1):
            return False
        elif [dx_abs,dy_abs,dz_abs].count(0)!=2:
            return False
        else:
            return True

    def _collapse_via_rule(self,coord_target,coord_impact):
        dx = coord_target[0]-coord_impact[0]
        dy = coord_target[1]-coord_impact[1]
        dz = coord_target[2]-coord_impact[2]
        if dx!=0:
            direction = 'xp' if dx>0 else 'xn'
        elif dy!=0:
            direction = 'yp' if dy>0 else 'yn'
        else:
            direction = 'zp' if dz>0 else 'zn'

        states_after_collapse = []
        #print(f'before collapse: {self.site[coord_target]}')
        for voc_code_impact in self.site[coord_impact]:
            voc_impact = dictionary[voc_code_impact]
            allowed = voc_impact.allowed[direction]

            temp_list = list(set(self.site[coord_target]) & set(allowed)) # collapse by set-intersection
            #if set(temp_list) != (self.site[coord_target]):
            #    print(f'collapse occurs: coord_target={coord_target}, coord_impact={coord_impact}, direction={direction}')
            #    print(f'voc_code_impact={voc_code_impact}, allowed={allowed}')
            #    print(f'temp_list (intersection): {temp_list}')
            temp_list.sort()
            #print(f'(within loop-) states_after_collapse = {states_after_collapse}')
            states_after_collapse = states_after_collapse[:] + temp_list
            #print(f'(within loop+) states_after_collapse = {states_after_collapse}')

        states_after_collapse = list(set(states_after_collapse))
        #print(f'(outside loop) states_after_collapse = {states_after_collapse}')
        states_after_collapse.sort()
        self.site[coord_target] = states_after_collapse[:]
        #print(f'after collapse: {self.site[coord_target]}')
        #print()

        if len(self.site[coord_target])==0:
            return False
        else:
            return True

## Testing
wfc = WFC()
wfc.reset_site(max_h=4, section=(3,3))
#pprint.pprint(wfc.site)
wfc.run_wfc()
wfc.print_visitable_space()
