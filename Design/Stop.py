import matplotlib.pyplot as plt
from enum import Enum
from matplotlib.widgets import Slider
import numpy as np

start_velocity = 0
distance = [250,-1000,5000]
acceleration_max = [2,2,25]
deceleration_max = [2,2,20]

velocity_max = 400

# this is not apply in the simulation for now
mass = 250000

acceleration_force = [288000*4, 288000*4, 3600000*2]
deceleration_force = [288000*4, 288000*4, 288000*8]

acceleration_max = []
deceleration_max = []

for force in acceleration_force:
    acceleration_max.append(force / mass)

for force in deceleration_force:
    deceleration_max.append(force / mass)

target_tolerance = 0.1
speed_margen = 0.5

# delta_t is the amount of time between each simulation cycle
# it is set to 1/60 as space engineers runs 60 ticks per second
delta_t = 1/60

def calc_stopping_distance(velocity, acceleration, deceleration):

    if velocity < 0:
        stopping_dist = -velocity*velocity/(2*acceleration)
    else:
        stopping_dist = velocity*velocity/(2*deceleration)

    return stopping_dist


def set_velocity(target_speed, curret_speed, acceleration, deceleration):

    delta_v = abs(curret_speed - target_speed) * 1/delta_t
    acceleration = min(acceleration, delta_v)
    deceleration = min(deceleration, delta_v)

    if target_speed < curret_speed:
        curret_speed = max(-velocity_max, curret_speed -
                           deceleration * delta_t)
    elif target_speed > curret_speed:
        curret_speed = min(velocity_max, curret_speed + acceleration * delta_t)

    return curret_speed

def asynchronous_range_check(values, span, offset = 0):
    check = True
    for value in values:
        if not value <= span + offset and value >= -span + offset:
            check = False
            break
    return check

def simulate(distance, start_velocity):

    time = 0
    velocity = [start_velocity] * 3

    acceleration = acceleration_max
    deceleration = deceleration_max

    target_distance = distance

    # variables for simulation output
    distance_data = []
    stoppig_data = []
    velocity_data = []
    target_velocity_data = []
    time_data = []

    while (not(asynchronous_range_check(target_distance, 1)) or not(asynchronous_range_check(velocity, 0.1))) and time < 500:
        time += delta_t

        # setup variables used by each axis
        stopping_distance = []
        target_velocity = []
        stopping_diff = []
        
        # ----------------------------------------------------------------------------------------------------- #
        # Calculate the control for each axis
        for i in range(3):
            # calcaulate the stopping distances
            stopping_distance.append(calc_stopping_distance(velocity[i], acceleration[i], deceleration[i]))

            # calculate the target velocity
            stopping_diff.append(target_distance[i] - stopping_distance[i])

        # normalize the velocity then scale with the velocity max
        # this will produce a velocity vector that is proportional distributed and the magnitude is velocity max
        target_velocity = np.array(target_distance)
        target_velocity = target_velocity / np.linalg.norm(target_velocity) * velocity_max
        target_velocity = np.ndarray.tolist(target_velocity)

        for i in range(3):
            target_velocity[i] = min(abs(target_velocity[i]), abs(stopping_diff[i])) * np.sign(target_velocity[i])
        # ----------------------------------------------------------------------------------------------------- #
        # Simulate the control
        for i in range(3):

            # calc velocity
            velocity[i] = set_velocity(target_velocity[i], velocity[i], acceleration[i], deceleration[i])

            # update the distance to target
            target_distance[i] -= velocity[i] * delta_t

        # record the data for this run
        distance_data.append(target_distance[:])
        velocity_data.append(velocity[:])
        target_velocity_data.append(target_velocity[:])
        stoppig_data.append(stopping_distance[:])
        time_data.append(time)

    return distance_data, stoppig_data, velocity_data, target_velocity_data, time_data


distance_data, stop_data, velocity_data, target_velocity, time = simulate(distance, start_velocity)


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
    plt.plot(time, get_list_index(velocity_data,i))
    plt.plot(time, get_list_index(target_velocity,i), 'r--')
    plt.xlabel('Time')
    plt.ylabel('Velocity')

    plt.subplot(3,2,2 + 2*i)
    plt.plot(time, get_list_index(distance_data,i))
    plt.plot(time, get_list_index(stop_data, i), 'r--')
    plt.xlabel('Time')
    plt.ylabel('Distance')

plt.show()
