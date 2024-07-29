cmake_minimum_required(VERSION 3.10)
project(psyonic_ability_hand_model)

find_package(catkin REQUIRED
  COMPONENTS
)

catkin_package(
  CATKIN_DEPENDS
)

install(DIRECTORY model/
  DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION}/model)
