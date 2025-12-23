using System.Collections.Generic;
using System.Linq;
using CondotelManagement.Data;
using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Booking;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly CondotelDbVer1Context _context;

        public BookingRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public IEnumerable<Booking> GetBookingsByCustomerId(int customerId)
        {
            return _context.Bookings
                .Include(b => b.Condotel)  // Thêm include để lấy tên condotel
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.EndDate)  // Sắp xếp mới nhất trước
                .ToList();
        }

        public Booking GetBookingById(int id)
            => _context.Bookings.FirstOrDefault(b => b.BookingId == id);

        public IEnumerable<Booking> GetBookingsByCondotel(int condotelId)
            => _context.Bookings.Where(b => b.CondotelId == condotelId).ToList();

        public void AddBooking(Booking booking)
        {
            _context.Bookings.Add(booking);
        }

        public void UpdateBooking(Booking booking)
        {
            _context.Bookings.Update(booking);
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<HostBookingDTO> GetBookingsByHost(int hostId)
        {
            return GetBookingsByHost(hostId, null);
        }

        public IEnumerable<HostBookingDTO> GetBookingsByHost(int hostId, BookingFilterDTO? filter)
        {
            var query = _context.Bookings
                .Where(b => b.Condotel.HostId == hostId)
                .AsQueryable();

            // Apply filters
            if (filter != null)
            {
                // Filter by search term (Booking ID, Customer Name, Condotel Name)
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var search = filter.SearchTerm.ToLower();
                    query = query.Where(b =>
                        b.BookingId.ToString().Contains(search) ||
                        b.Customer.FullName.ToLower().Contains(search) ||
                        b.Condotel.Name.ToLower().Contains(search) ||
                        (b.Customer.Email != null && b.Customer.Email.ToLower().Contains(search)) ||
                        (b.Customer.Phone != null && b.Customer.Phone.Contains(search)));
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status.ToLower() != "all")
                {
                    query = query.Where(b => b.Status == filter.Status);
                }

                // Filter by condotel ID
                if (filter.CondotelId.HasValue)
                {
                    query = query.Where(b => b.CondotelId == filter.CondotelId.Value);
                }

                // Filter by booking date (CreatedAt)
                if (filter.BookingDateFrom.HasValue)
                {
                    query = query.Where(b => b.CreatedAt >= filter.BookingDateFrom.Value);
                }
                if (filter.BookingDateTo.HasValue)
                {
                    var endDate = filter.BookingDateTo.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(b => b.CreatedAt <= endDate);
                }

                // Filter by start date (check-in)
                if (filter.StartDateFrom.HasValue)
                {
                    query = query.Where(b => b.StartDate >= filter.StartDateFrom.Value);
                }
                if (filter.StartDateTo.HasValue)
                {
                    query = query.Where(b => b.StartDate <= filter.StartDateTo.Value);
                }

                // Filter by end date (check-out)
                if (filter.EndDateFrom.HasValue)
                {
                    query = query.Where(b => b.EndDate >= filter.EndDateFrom.Value);
                }
                if (filter.EndDateTo.HasValue)
                {
                    query = query.Where(b => b.EndDate <= filter.EndDateTo.Value);
                }
            }

            // Apply sorting
            var sortBy = filter?.SortBy?.ToLower() ?? "bookingDate";
            var sortDescending = filter?.SortDescending ?? true;

            var bookings = query.Select(b => new HostBookingDTO
            {
                BookingId = b.BookingId,
                CustomerName = b.Customer.FullName,
                CustomerPhone = b.Customer.Phone,
                CustomerEmail = b.Customer.Email,
                CondotelName = b.Condotel.Name,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                BookingDate = b.CreatedAt,
                TotalPrice = b.TotalPrice,
                Status = b.Status,

                Services = b.BookingDetails
                    .Select(d => new BookingServiceDTO
                    {
                        ServiceName = d.Service.Name,
                        Quantity = d.Quantity,
                        Price = d.Price
                    }).ToList(),
                    
                // Thông tin người được đặt hộ
                GuestFullName = b.GuestFullName,
                GuestPhone = b.GuestPhone,
                GuestIdNumber = b.GuestIdNumber,
                
                // Check-in token
                CheckInToken = b.CheckInToken,
                CheckInTokenGeneratedAt = b.CheckInTokenGeneratedAt,
                CheckInTokenUsedAt = b.CheckInTokenUsedAt
            });

            // Apply sorting
            switch (sortBy)
            {
                case "startdate":
                    bookings = sortDescending 
                        ? bookings.OrderByDescending(x => x.StartDate)
                        : bookings.OrderBy(x => x.StartDate);
                    break;
                case "enddate":
                    bookings = sortDescending
                        ? bookings.OrderByDescending(x => x.EndDate)
                        : bookings.OrderBy(x => x.EndDate);
                    break;
                case "totalprice":
                    bookings = sortDescending
                        ? bookings.OrderByDescending(x => x.TotalPrice ?? 0)
                        : bookings.OrderBy(x => x.TotalPrice ?? 0);
                    break;
                case "bookingdate":
                default:
                    bookings = sortDescending
                        ? bookings.OrderByDescending(x => x.BookingDate)
                        : bookings.OrderBy(x => x.BookingDate);
                    break;
            }

            return bookings.ToList();
        }

        public IEnumerable<HostBookingDTO> GetBookingsByHostAndCustomer(int hostId, int customerId)
        {
            return _context.Bookings
            .Where(b => b.Condotel.HostId == hostId && b.Customer.UserId == customerId)
            .Select(b => new HostBookingDTO
            {
                BookingId = b.BookingId,
                CustomerName = b.Customer.FullName,
                CustomerPhone = b.Customer.Phone,
                CustomerEmail = b.Customer.Email,
                CondotelName = b.Condotel.Name,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                BookingDate = b.CreatedAt,
                TotalPrice = b.TotalPrice,
                Status = b.Status,

                Services = b.BookingDetails
                    .Select(d => new BookingServiceDTO
                    {
                        ServiceName = d.Service.Name,
                        Quantity = d.Quantity,
                        Price = d.Price
                    }).ToList(),
                    
                // Thông tin người được đặt hộ
                GuestFullName = b.GuestFullName,
                GuestPhone = b.GuestPhone,
                GuestIdNumber = b.GuestIdNumber,
                
                // Check-in token
                CheckInToken = b.CheckInToken,
                CheckInTokenGeneratedAt = b.CheckInTokenGeneratedAt,
                CheckInTokenUsedAt = b.CheckInTokenUsedAt
            })
            .OrderByDescending(x => x.StartDate)
            .ToList();
        }
    }
}
