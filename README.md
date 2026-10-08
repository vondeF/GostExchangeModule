# <img width="30" height="30" alt="icons8-база-данных" src="https://github.com/user-attachments/assets/3b3a9f6d-9fb2-4fb3-9b8b-427ba338750a" /> GostExchangeModule
Специальный модуль экспорта/импорта по ГОСТ Р 58651 необходим для экспорта и импорта данных согласно актуальному состоянию стандартов серии ГОСТ Р 58651.

С помощью модуля осуществляется:
* Экспорт фрагментов информационной модели ИМ;
* Экспорт наборов изменений ИМ;
* Импорт фрагментов ИМ;
* Импорт наборов изменений ИМ;

## Особенности работы
Модуль осуществляет наполнение данных, игнорирование объектов выбранных классов, атрибутов и ассоциаций, а также преобразование данных "на лету".
Основная функциональность реализована в преобразовании классов солнечных и ветровых электростанций (СЭС и ВЭС) для приведения данных из формата ПО-источника данных в формат, описанный в стандартах ГОСТ Р 58651.

### Маппинг свойств преобразуемых классов:
**PhotoVoltaicUnit → ThermalGeneratingUnit**
| Атрибут PhotoVoltaicUnit | Атрибут ThermalGeneratingUnit |
| ------------------------ | ----------------------------- |
| so.PhotoVoltaicUnit.trackerType | so.ThermalGeneratingUnit.trackerType |
| rf.PowerElectronicsUnit.governorSCD | cim.GeneratingUnit.governorSCD |
| cim.PowerElectronicsUnit.maxP | cim.GeneratingUnit.maxOperatingP |
| cim.PowerElectronicsUnit.minP | cim.GeneratingUnit.minOperatingP |


| Ассоциация PhotoVoltaicUnit | Ассоциация ThermalGeneratingUnit |
| ------------------------ | ----------------------------- |
| cim.PowerElectronicsUnit.PowerElectronicsConnection | cim.GeneratingUnit.RotatingMachine |


**PowerElectronicsConnection → SynchronousMachine**
| Атрибут PowerElectronicsConnection | Атрибут SynchronousMachine |
| ------------------------ | ----------------------------- |
| cim.PowerElectronicsConnection.maxQ | cim.SynchronousMachine.maxQ |
| cim.PowerElectronicsConnection.minQ | cim.SynchronousMachine.minQ |
| rf.PowerElectronicsConnection.ratedPowerFactor | cim.RotatingMachine.ratedPowerFactor |
| cim.PowerElectronicsConnection.ratedS | cim.RotatingMachine.ratedS |
| cim.PowerElectronicsConnection.ratedU | cim.RotatingMachine.ratedU |
| rf.PowerElectronicsConnection.type | cim.SynchronousMachine.type = generator |
| - | cim.SynchronousMachine.operatingMode = generator |


| Ассоциация PowerElectronicsConnection | Ассоциация SynchronousMachine |
| ------------------------ | ----------------------------- |
| cim.PowerElectronicsConnection.PowerElectronicsUnit | cim.RotatingMachine.GeneratingUnit |
| rf.PowerElectronicsConnection.PowerElectronicsReactiveCapabilityCurve  | cim.SynchronousMachine.InitialReactiveCapabilityCurve, cim.SynchronousMachine.ReactiveCapabilityCurves |


**WindGeneratingUnit → ThermalGeneratingUnit**
| Атрибут WindGeneratingUnit | Атрибут ThermalGeneratingUnit |
| ------------------------ | ----------------------------- |
| - | - |


| Ассоциация WindGeneratingUnit | Ассоциация ThermalGeneratingUnit |
| ------------------------ | ----------------------------- |
| - | - |


**PowerElectronicsWindUnit → ThermalGeneratingUnit**
| Атрибут PowerElectronicsWindUnit | Атрибут ThermalGeneratingUnit |
| ------------------------ | ----------------------------- |
| rf.PowerElectronicsUnit.governorSCD | cim.GeneratingUnit.governorSCD |
| cim.PowerElectronicsUnit.maxP | cim.GeneratingUnit.maxOperatingP |
| cim.PowerElectronicsUnit.minP | cim.GeneratingUnit.minOperatingP |


| Ассоциация PowerElectronicsWindUnit | Ассоциация ThermalGeneratingUnit |
| ------------------------ | ----------------------------- |
| cim.PowerElectronicsUnit.PowerElectronicsConnection | cim.GeneratingUnit.RotatingMachine  |


**AsynchronousMachine → SynchronousMachine**
| Атрибут AsynchronousMachine | Атрибут SynchronousMachine |
| ------------------------ | ----------------------------- |
| - | cim.SynchronousMachine.type = generator |
| - | cim.SynchronousMachine.operatingMode = generator |


| Ассоциация AsynchronousMachine | Ассоциация SynchronousMachine |
| ------------------------ | ----------------------------- |
| rf.AsynchronousMachine.ReactiveCapabilityCurve  | cim.SynchronousMachine.InitialReactiveCapabilityCurve, cim.SynchronousMachine.ReactiveCapabilityCurves |


**PowerElectronicsReactiveCapabilityCurve → ReactiveCapabilityCurve**
| Атрибут PowerElectronicsReactiveCapabilityCurve | Атрибут ReactiveCapabilityCurve |
| ------------------------ | ----------------------------- |
| - | - |


| Ассоциация PowerElectronicsReactiveCapabilityCurve | Ассоциация ReactiveCapabilityCurve |
| ------------------------ | ----------------------------- |
| rf.PowerElectronicsReactiveCapabilityCurve.PowerElectronicsConnection | cim.ReactiveCapabilityCurve.InitiallyUsedBySynchronousMachines, cim.ReactiveCapabilityCurve.SynchronousMachines |


**AsynchronousMachineReactiveCapabilityCurve → ReactiveCapabilityCurve**
| Атрибут AsynchronousMachineReactiveCapabilityCurve | Атрибут ReactiveCapabilityCurve |
| ------------------------ | ----------------------------- |
| - | - |


| Ассоциация AsynchronousMachineReactiveCapabilityCurve | Ассоциация ReactiveCapabilityCurve |
| ------------------------ | ----------------------------- |
| cim.AsynchronousMachineReactiveCapabilityCurve.AsynchronousMachine  | cim.ReactiveCapabilityCurve.InitiallyUsedBySynchronousMachines, cim.ReactiveCapabilityCurve.SynchronousMachines |
