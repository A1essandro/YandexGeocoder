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
IDictionary<string, IEnumerable<GeoPoint>> pointsToAddress await geocoder.GetPointsByAddresses(addresses);
```
## Кэширование
В конструктор `Geocoder` можно передать реализацию интерфейса `IRTech.YandexGeocoder.CacheProvider.ICacheProvider` в качестве первого параметра. Реализация `Geocoder` гарантирует, что не будет вызван одновременно метод вставки и получения данных в провайдер.
## Лицензия
[MIT](https://raw.githubusercontent.com/A1essandro/YandexGeocoder/master/LICENSE)