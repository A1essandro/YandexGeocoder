[![Build status](https://ci.appveyor.com/api/projects/status/hkx2qtcbu686qc8g/branch/master?svg=true)](https://ci.appveyor.com/project/A1essandro/yandexgeocoder/branch/master) [![Build Status](https://travis-ci.org/A1essandro/YandexGeocoder.svg?branch=master)](https://travis-ci.org/A1essandro/YandexGeocoder) ![NuGet](https://img.shields.io/nuget/v/IRTech.YandexGeocoder.svg) ![NuGet Pre Release](https://img.shields.io/nuget/vpre/IRTech.YandexGeocoder.svg)

# IRTech.YandexGeocoder

> Внимание! Прежде чем использовать данную библиотеку, прочтите, пожалуйста, [условия использования Яндекс.Геокодера](https://tech.yandex.ru/maps/geocoder/#terms). Не используйте кэширование на срок более 30 дней.

## Примеры использования
Получение самого вероятного результата:
```csharp
using IRTech.YandexGeocoder;
//...................................................
Geocoder geocoder = new Geocoder();
GeoPoint samara = await geocoder.GetPoint("Самара");
Console.WriteLine(samara.Latitude); //53.195538
Console.WriteLine(samara.Longitude); //50.101783
```
Получение всех подходящих результатов:
```csharp
IEnumerable<GeoPoint> samara = await geocoder.GetPoints("Брест");
```
Подобным образом можно получить списки точек для списка адресов, либо по одной точке, соответствущей каждому адресу из списка:
```csharp
IEnumerable<string> addresses;
//...............................................
IDictionary<string, GeoPoint> pointToAddress = await geocoder.GetPointByAddresses(addresses);
IDictionary<string, IEnumerable<GeoPoint>> pointsToAddress = await geocoder.GetPointsByAddresses(addresses);
```
## Кэширование
В конструктор `Geocoder` можно передать реализацию интерфейса `IRTech.YandexGeocoder.CacheProvider.ICacheProvider` в качестве первого параметра. Реализация `Geocoder` гарантирует, что не будет вызван одновременно метод вставки и получения данных в провайдер.
## Лицензия
[MIT](https://raw.githubusercontent.com/A1essandro/YandexGeocoder/master/LICENSE)