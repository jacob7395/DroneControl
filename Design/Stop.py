import matplotlib.pyplot as plt
from enum import Enum
from matplotlib.widgets import Slider
import numpy

start_velocity = 0
distance = 10000
mass = 5000
target_tolorance = 0.1
max_speed = 400

speed_margen = 0.5

acceletraion_max = 25
deccleration_max = 25
velocity_max = 400

delta_t = 1/60


def calc_stopping_distance(velocity, deccleration):
    stopping_dist = velocity*velocity/(2*deccleration_max)

    if velocity < 0:
        stopping_dist *= -1

    return stopping_dist


def set_velocity(target_speed, curret_speed, acceleration, decceleration):

    if target_speed < curret_speed:
        curret_speed = max(-velocity_max, curret_speed -
                           decceleration * delta_t)
    elif target_speed > curret_speed:
        curret_speed = min(velocity_max, curret_speed + acceleration * delta_t)

    return curret_speed

def asyncrones_range_check(value, range, offset = 0):
    return value < range + offset and value > -range + offset

def simulate_travel(distance, start_velocity):

    time = 0
    traveled = 0
    velocitey = start_velocity

    class motion_state(Enum):
        accelerating = 1
        deccelerating = 2
        stop = 3
        holding = 4

    travlace_state = motion_state.accelerating
    acceleration = acceletraion_max
    deccelration = deccleration_max

    distance_data = []
    stoppig_data = []
    velocitey_data = []
    time_data = []

    switched = True

    target_distance = distance
    while (not(asyncrones_range_check(target_distance, 1)) or not(asyncrones_range_check(velocitey, 0.1))) and time < 500:
        time += delta_t

        stopping_distance = calc_stopping_distance(velocitey, deccleration_max)

        targed_velocity = target_distance - stopping_distance

        velocitey = set_velocity(targed_velocity, velocitey, acceleration, deccelration)

        target_distance -= velocitey * delta_t

        distance_data.append(target_distance)
        velocitey_data.append(velocitey)
        stoppig_data.append(stopping_distance)
        time_data.append(time)

        if asyncrones_range_check(target_distance, 1) and not switched:
            target_distance = -distance
            switched = True

    return distance_data, stoppig_data, velocitey_data, time_data


distance_data, stop_data, velocitey_data, time = simulate_travel(
    distance, start_velocity)

plt.figure(1)
plt.subplot(211)
plt.plot(time, velocitey_data)
plt.xlabel('Time')
plt.ylabel('Velocity')

plt.figure(1)
plt.subplot(212)
plt.plot(time, distance_data)
plt.plot(time, stop_data, 'r--')
plt.xlabel('Time')
plt.ylabel('Distance')


plt.show()
