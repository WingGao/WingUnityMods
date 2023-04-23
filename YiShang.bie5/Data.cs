using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;

namespace YiShang.WingMod.bie5
{
    //城市基础数据
    public class YCityStatic
    {
        public int rank;
        public string belong;
    }

    //城市当前数据
    public class YCity
    {
        public string cityName;
        public int comLv; //等级
        public bool allowBuy;
        public int needDay; //前往天数
        public List<YGood> goodsData;
    }

    // 商品数据
    public class YGood
    {
        public string goodName;
        public int inventory; //当前库存
        public int value; //价格
    }

    // 任务数据
    public class YRenwu
    {
        public string name;
        public string targetLocation; //城市key
        public List<string> goodList;
        public List<int> needList;
        public Dictionary<string, int> award;
    }

    class DynamicDictionaryWrapper : DynamicObject
    {
        protected readonly Dictionary<string, object> _source;

        public DynamicDictionaryWrapper(Dictionary<string, object> source)
        {
            _source = source;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            return (_source.TryGetValue(binder.Name, out result));
        }
    }

    public static class Ext
    {
        public static T ConvertTo<T>(this Dictionary<string, object> dic)
        {
            var j = JsonConvert.SerializeObject(dic);
            return JsonConvert.DeserializeObject<T>(j);
        }
    }
}