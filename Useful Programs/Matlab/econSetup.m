%% User Input
clear
initialSetup = true; %Initial setup to set quantity and blacklist 
tradeZoneSetup = true; 
%%
%Setup File Path
[econConfigFile, econDataFile] = pathsetup; 

%Read in Economy Setup Files (Set to UTF-8 in XML file instead of UTF-16)
[econConfig] = xml2struct(econConfigFile); 
[econData] = xml2struct(econDataFile); 

%Extract Default Prices
marketItem = econConfig.EconConfig.DefaultPrices.MarketItem; 
itemNames = arrayfun(@(x) marketItem{x}.SubtypeName.Text, 1:length(marketItem), 'UniformOutput', false)';

%Load Economy Config Spreadsheet 
[~,~,setEcon] = xlsread('SE_Economy.xlsx', 'Prices'); 
setItems = setEcon(:,3); 
setDefault = setEcon(:,4); 
setBuyF1 = setEcon(:,5); 
setSellF1 = setEcon(:,6); 
setBuyF2 = setEcon(:,7); 
setSellF2 = setEcon(:,8); 
setBuyF3 = setEcon(:,9); 
setSellF3 = setEcon(:,10); 
setBuyF4 = setEcon(:,11); 
setSellF4 = setEcon(:,12); 

%Convert Numerical Cells to String 
setDefault = cellfun(@(x) num2str(x), setDefault, 'UniformOutput', false); 
Freeport(1).buy = cellfun(@(x) num2str(x), setBuyF1, 'UniformOutput', false); 
Freeport(1).sell = cellfun(@(x) num2str(x), setSellF1, 'UniformOutput', false); 
Freeport(2).buy = cellfun(@(x) num2str(x), setBuyF2, 'UniformOutput', false); 
Freeport(2).sell = cellfun(@(x) num2str(x), setSellF2, 'UniformOutput', false); 
Freeport(3).buy = cellfun(@(x) num2str(x), setBuyF3, 'UniformOutput', false); 
Freeport(3).sell = cellfun(@(x) num2str(x), setSellF3, 'UniformOutput', false); 
Freeport(4).buy = cellfun(@(x) num2str(x), setBuyF4, 'UniformOutput', false); 
Freeport(4).sell = cellfun(@(x) num2str(x), setSellF4, 'UniformOutput', false); 

%Tag Ore and Ingots 
itemNames(24:33) = cellfun(@(x) [x, 'Ore'], itemNames(24:33), 'UniformOutput', false);
itemNames(34:43) = cellfun(@(x) [x, 'Ingot'], itemNames(34:43), 'UniformOutput', false);

for ii = 1:length(itemNames) %For each item in the config 
    
    [match] = find(strcmpi(setItems,itemNames{ii})); %Find the matching item
    
    if ~isempty(match)        
        %Set Default Buy Price
        econConfig.EconConfig.DefaultPrices.MarketItem{ii}.BuyPrice = setDefault{match}; 
        %Set Default Sell Price 
        econConfig.EconConfig.DefaultPrices.MarketItem{ii}.SellPrice = setDefault{match}; 
        
        %Set Blacklist
        if initialSetup 
        econConfig.EconConfig.DefaultPrices.MarketItem{ii}.IsBlacklisted.Text = 'false'; 
        %Set Quantity
        econConfig.EconConfig.DefaultPrices.MarketItem{ii}.Quantity.Text = '1000000000';
        end 
        
        %Trade Zones (Note trade zones must already be set up 
        if tradeZoneSetup 
            for tz = 1:4               
                %Set Default Buy Price
                econData.EconData.Markets.Market{tz+1}.MarketItems.MarketItem{ii}.BuyPrice = Freeport(tz).buy{match}; 
                %Set Default Sell Price 
                econData.EconData.Markets.Market{tz+1}.MarketItems.MarketItem{ii}.SellPrice = Freeport(tz).sell{match}; 

                %Set Blacklist
                if initialSetup 
                econData.EconData.Markets.Market{tz+1}.MarketItems.MarketItem{ii}.IsBlacklisted.Text = 'false'; 
                %Set Quantity
                econData.EconData.Markets.Market{tz+1}.MarketItems.MarketItem{ii}.Quantity.Text = '1000000000';
                end 
            end
            
        end
        
    else
        disp([itemNames{ii}, ' is not set'])
    end 
        
    
end

%Write Economy Config to File 
struct2xml(econConfig, 'EconomyConfig.xml'); 

%Write Economy Data to File 
struct2xml(econData, 'EconomyData.xml'); 






    