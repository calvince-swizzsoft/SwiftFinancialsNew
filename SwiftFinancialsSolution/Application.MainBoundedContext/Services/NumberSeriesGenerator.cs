using Domain.MainBoundedContext.AccountsModule.Aggregates.NumberSeriesAgg;
using Domain.Seedwork;
using Domain.Seedwork.Specification;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Application.MainBoundedContext.Services
{
    // Specification for finding NumberSeries by Code
    public class NumberSeriesByCodeSpec : Specification<NumberSeries>
    {
        private readonly string _code;

        public NumberSeriesByCodeSpec(string code)
        {
            _code = code;
        }

        public override Expression<Func<NumberSeries, bool>> SatisfiedBy()
        {
            return ns => ns.Code == _code;
        }
    }

    public class NumberSeriesGenerator : INumberSeriesGenerator
    {
        private readonly IRepository<NumberSeries> _numberSeriesRepository;

        public NumberSeriesGenerator(IRepository<NumberSeries> numberSeriesRepository)
        {
            _numberSeriesRepository = numberSeriesRepository;
        }

        public string GetNextNumber(string code, ServiceHeader serviceHeader)
        {
            // Use specification to find the number series
            var spec = new NumberSeriesByCodeSpec(code);
            var seriesList = _numberSeriesRepository.AllMatching(spec, serviceHeader);
            var series = seriesList.FirstOrDefault();

            if (series == null)
                throw new ArgumentException($"Number series code '{code}' not found.");

            // Clone the original for Merge
            var original = CloneNumberSeries(series);

            // Increment the number
            series.LastUsedNumber += 1;

            // Update using Merge
            _numberSeriesRepository.Merge(original, series, serviceHeader);

            // Format like PV00001
            string nextNumber = $"{series.Prefix}{series.LastUsedNumber.ToString().PadLeft(series.Padding, '0')}";

            return nextNumber;
        }

        private NumberSeries CloneNumberSeries(NumberSeries source)
        {
            return new NumberSeries
            {
                //Id = source.Id,
                Code = source.Code,
                Prefix = source.Prefix,
                LastUsedNumber = source.LastUsedNumber,
                Padding = source.Padding
            };
        }
    }
}