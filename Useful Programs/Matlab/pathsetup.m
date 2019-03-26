function [EconConfig, EconData] = pathsetup()
%PATHSETUP Summary of this function goes here
%   Detailed explanation goes here

addpath(genpath(pwd))

EconConfig = '/economyConfig/EconomyConfig.xml'; 

EconData = '/economyConfig/EconomyData.xml'; 

end

