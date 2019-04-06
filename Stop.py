import matplotlib.pyplot as plt
from enum import Enum
from matplotlib.widgets import Slider
import numpy as np

start_velocity = 0
distance = [250, -100, 5000]

velocity_max = 400

# this is not applyed in the simulation for now
mass = 250000/2

acceleration_force = [288000*4,288000*4,3600000*2]
decceleration_force = [288000*4,288000*4,288000*8]

acceletraion_max = []
deacceleration_max = []

for force in acceleration_force:
    acceletraion_max.append(force / mass)

for force in decceleration_force:
    deacceleration_max.append(force / mass)

target_tolorance = 0.1
speed_margen = 0.5

# delta_t is the amount of time between each simulation cycle
# it is set to 1/60 as space engeneeries runs 60 ticks per second
delta_t = 1/60

def calc_stopping_distance(velocity, acceleration, decceleration):

    if velocity < 0:
        stopping_dist = -velocity*velocity/(2*acceleration)
    else:
        stopping_dist = velocity*velocity/(2*decceleration)

    return stopping_dist


def set_velocity(target_speed, curret_speed, acceleration, decceleration):

    delta_v = abs(curret_speed - target_speed) * 1/delta_t
    acceleration = min(acceleration, delta_v)
    decceleration = min(decceleration, delta_v)

    if target_speed < curret_speed:
        curret_speed = max(-velocity_max, curret_speed -
                           decceleration * delta_t)
    elif target_speed > curret_speed:
        curret_speed = min(velocity_max, curret_speed + acceleration * delta_t)

    return curret_speed

def asyncrones_range_check(values, span, offset = 0):
    check = True
    for value in values:
        if not (value <= span + offset and value >= -span + offset):
            check = False
            break
    return check

def simulate(distance, start_velocity):

    time = 0
    velocitey = [start_velocity] * 3

    acceleration = acceletraion_max
    deccelration = deacceleration_max

    target_distance = distance

    # varibles for simulation output
    distance_data = []
    stoppig_data = []
    velocitey_data = []
    target_velocitey_data = []
    time_data = []

    simulating = True
    extra_time = 2.5
    while simulating and time < 500:
        time += delta_t

        # setup varibles used by each axis
        stopping_distance = []
        target_velocity = []
        stopping_diff = []

        # ----------------------------------------------------------------------- #
        # Calculate the control for each axis
        for i in range(3):
            # calcaulate the stopping distances
            stopping_distance.append(calc_stopping_distance(velocitey[i], acceleration[i], deccelration[i]))

            # calculte the target velocitys
            stopping_diff.append(target_distance[i] - stopping_distance[i])

        # nomolize the velocity then scale with the velocity max
        # this will produce a velocity vector that is prposonaly distrabuted and the magnitude is velocity max
        target_velocity = np.array(stopping_diff)

        #target_velocity[2] -= target_velocity[2] * 0.95

        target_velocity = target_velocity / np.linalg.norm(target_velocity) * velocity_max
        target_velocity = np.ndarray.tolist(target_velocity)

        for i in range(3):
            target_velocity[i] = min(abs(target_velocity[i]), abs(stopping_diff[i])) * np.sign(target_velocity[i])
        # ----------------------------------------------------------------------- #
        # Simulate the control
        for i in range(3):

            # calc velocitys
            velocitey[i] = set_velocity(target_velocity[i], velocitey[i], acceleration[i], deccelration[i])

            # update the distance to target
            target_distance[i] -= velocitey[i] * delta_t

        # record the data for this run
        distance_data.append(target_distance[:])
        velocitey_data.append(velocitey[:])
        target_velocitey_data.append(target_velocity[:])
        stoppig_data.append(stopping_distance[:])
        time_data.append(time)

        # check if simulation goals have been met
        if asyncrones_range_check(target_distance, 0.01) and asyncrones_range_check(velocitey, 0.01):
            extra_time -= delta_t
            if extra_time <= 0:
                simulating = False
        else:
            extra_time = 2.5


    return distance_data, stoppig_data, velocitey_data, target_velocitey_data, time_data



def main():
    
    distance_data, stop_data, velocitey_data, target_velocity, time = simulate(distance, start_velocity)


    def get_list_index(data, index):
        """ """
        out = []
        for row in data:
            out.append(row[index])

        return out

    plt.figure(1)
    # plot data
    for i in range(3):
        plt.subplot(3,2,1 + 2*i)
        plt.plot(time, get_list_index(velocitey_data,i))
        plt.plot(time, get_list_index(target_velocity,i), 'r--')
        plt.xlabel('Time')
        plt.ylabel('Velocity')

        plt.subplot(3,2,2 + 2*i)
        plt.plot(time, get_list_index(distance_data,i))
        plt.plot(time, get_list_index(stop_data, i), 'r--')
        plt.xlabel('Time')
        plt.ylabel('Distance')

    plt.show()

if __name__ == '__main__':
    main()