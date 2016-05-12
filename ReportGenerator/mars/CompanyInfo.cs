using System;
using System.Collections.Generic;

namespace ReportGenerator
{
    public class CompanyInfo
    {
        public string Name;
        public string FullName;
        public string LatName;

        public string OGRN;
        public string INN;
        public string KPP;
        public string OKPO;
        public string OtherCodes;
        public string FSS;
        public string UstFond;
        public string History;
        // Information about existing bailiffs info, arbitr, contracts, licensies, trademark
        public bool BailiffsExist; // Исп. производства
        public bool ArbitrationExists; // Арбитраж
        public bool ContractsExist; // Госконтракты
        public bool LicensiesExist; // Лицензии
        public bool TrademarksExist; // Тов. знаки

        public string Status;
        // Current address with date
        public string Address;
        public DateTime AddressAddedDate;
        public int AddressCount;

        // Date of Manager
        public DateTime ManagerAddedDate;
        public string ManagerName;
        public string ManagerAmplua;
        public int ManagerCount;
        public string ManagerINN;

        public List<string> PhoneNumbers = new List<string>();
        public string NalogCode;
        public string RegDate;

        
        // Founders count
        public long FoundersCount;

        // Main activity code
        public string MainActiviyCode; // ОКВЭД

        // Arbitr counters
        public long ArbitrDefendantCount; // Количество дел в качестве ответчика за всё время
        public long ArbitrPlaintiffCount; // Количество дел в качестве истца за всё время
        public long ArbitrOtherCount; // Количество дел в другом качестве за всё время
        public long ArbitrBankruptcyCount; // Дела о банкротстве

        // Contract counters
        public long WonContractsCount;
        public long PlacedContractsCount;

        // History: previous names and addresses with date
        public List<ValueWithDate> OtherNames = new List<ValueWithDate>();
        public List<ValueWithDate> OtherAddresses = new List<ValueWithDate>();
        public List<Manager> LastManagers=new List<Manager>();
        public List<Founder> LastFounders = new List<Founder>();
        public List<ValueWithDate> LastFonds = new List<ValueWithDate>();

        public List<Founder> Founders;
        public List<Licency> Lics;
        public ContractsInfo WonContracts;
        public ContractsInfo PostedContracts;
        public BailiffsInfo BailiffsInfo;

        public ArbitrStat ArbitrAsPlaintiff;
        public ArbitrStat ArbitrAsRespondent;
        public ArbitrStat ArbitrAsThird;

        public string FinYear;
        public string FinBalance;
        public string FinProfit;
        public string FinNetProfit;

        public List<Activity> Activities;

        public List<RelatedCompany> Predecessors;
        public List<RelatedCompany> RelatedCompanies;
        public string SpecialReestrs;

        public List<string> BankruptMessages;
    }
    public class Manager
    {
        public string Name;
        public string Amplua;
        public DateTime Date;
        public string INN;
        public int Count;
    }
    public class Founder
    {
        public string Name;
        public string Percent;
        public string Rubles;
        public string OGRN;
        public string INN;
        public DateTime Date;
    }
    public class Activity
    {
        public string Code;
        public string Name;
    }
    public class ArbitrStat
    {
        public int Count;
        public int LastYearCount;
        public string Sum;
        public List<ArbitrCase> Cases = new List<ArbitrCase>();
    }
    public class ArbitrCase
    {
        public string Number;
        public DateTime Date;
        public string Sum;
        public string Plaintiff;
        public string Respondent;
        public string Thirds;
        public string Type;

        Dictionary<string, DateTime> Instances = new Dictionary<string, DateTime>();

    }
    public class BailiffsInfo
    {
        public string Sum;
        public int Count;
        public List<BailiffsCase> Cases = new List<BailiffsCase>();
    }
    public class BailiffsCase
    {
        public string Number;
        public DateTime Date;
        public string Sum;
        public string Type;
    }
    public class ValueWithDate
    {
        public string Value;
        public DateTime AddedDate;

        public ValueWithDate(string value, DateTime date)
        {
            Value = value;
            AddedDate = date;
        }

        public override string ToString()
        {
            return AddedDate.ToShortDateString() + " | " + Value;
        }
    }

    public class Licency
    {
        public string Number;
        public DateTime? Date;
        public DateTime? Period;
        public string Activity;
        public string Department;
        public string Address;
        public string Status;

    }
    public class ContractsInfo
    {
        public int Count;
        public string Sum;
        public List<Contract> Contracts = new List<Contract>();
    }
    public class Contract
    {
        public string Name;
        public DateTime Date;
        public string Number;
        public string Status;
        public string Sum;
        public string TimeInterval;
        public string Description;
    }

    public class RelatedCompany
    {
        public string Name;
        public string INN;
        public string OGRN;
        public string Address;
        public string Status;
        public string Manager;
        public string ManagerDate;
        public string ManagerCount;

        public List<Founder> Foudners;
        public List<Founder> Founded;
        public string Bailiffs;
        public string AsPlaintiff;
        public string AsRespondent;
        public string Contracts;

        public string Capital;
    }
    public class SpecialReestr
    {
        public List<Item> Items;
        public class Item
        {
            public string Name;
            public string Value;
            public string Descr;
            public int Count;
        }
    }

}
