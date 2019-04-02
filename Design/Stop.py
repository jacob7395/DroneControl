import matplotlib.pyplot as plt
from enum import Enum
from matplotlib.widgets import Slider

start_velocity = 0
distance = 100
mass = 5000
target_tolorance = 0.1
max_speed = 400

speed_margen = 0.5

acceletraion_max = 1
deccleration_max = 0.5
velocity_max = 10

delta_t = 0.01


def calc_stopping_distance(velocity, deccleration):
    return velocity*velocity/(2*deccleration_max)


def set_velocity(target_speed, curret_speed, acceleration, decceleration):

    if target_speed < curret_speed:
        curret_speed = max(-velocity_max, curret_speed -
                           decceleration * delta_t)
    elif target_speed > curret_speed:
        curret_speed = min(velocity_max, curret_speed + acceleration * delta_t)

    return curret_speed


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

    while distance > 0:
        time += delta_t

        if travlace_state == motion_state.accelerating:
            velocitey = set_velocity(
                velocity_max, velocitey, acceleration, deccelration)
        elif travlace_state == motion_state.deccelerating:
            velocitey = set_velocity(
                -velocity_max, velocitey, acceleration, deccelration)
        else:
            velocitey = velocitey

        stopping_distance = calc_stopping_distance(velocitey, deccleration_max)

        distance -= velocitey * delta_t

        if stopping_distance >= distance:
            travlace_state = motion_state.deccelerating

        distance_data.append(distance)
        velocitey_data.append(velocitey)
        stoppig_data.append(stopping_distance)
        time_data.append(time)

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
