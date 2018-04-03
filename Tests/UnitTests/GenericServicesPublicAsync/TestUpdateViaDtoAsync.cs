﻿// Copyright (c) 2018 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT licence. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.EfClasses;
using DataLayer.EfCode;
using GenericServices;
using GenericServices.Configuration;
using GenericServices.PublicButHidden;
using GenericServices.Startup;
using Tests.Helpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Tests.UnitTests.GenericServicesPublicAsync
{
    public class TestUpdateViaDtoAsync
    {
        public class AuthorDto : ILinkToEntity<Author>
        {
            public int AuthorId { get; set; }
            [ReadOnly(true)]
            public string Name { get; set; }
            public string Email { get; set; }
        }

        [Fact]
        public async Task TestUpdateViaAutoMapperOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<EfCoreContext>();
            using (var context = new EfCoreContext(options))
            {
                context.Database.EnsureCreated();
                context.Add(new Author {Name = "Start Name", Email = "me@nospam.com"});
                context.SaveChanges();

                var wrapped = context.SetupSingleDtoAndEntities<AuthorDto>();
                var service = new GenericServiceAsync(context, wrapped);

                //ATTEMPT
                var dto = new AuthorDto { AuthorId = 1, Name = "New Name", Email = "you@gmail.com" };
                await service.UpdateAndSaveAsync(dto);

                //VERIFY
                service.IsValid.ShouldBeTrue(service.GetAllErrors());
                service.Message.ShouldEqual("Successfully updated the Author");
                var entity = context.Authors.Find(1);
                entity.Name.ShouldEqual("Start Name");
                entity.Email.ShouldEqual(dto.Email);
            }
        }

        [Fact]
        public async Task TestUpdateViaDefaultMethodOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<EfCoreContext>();
            using (var context = new EfCoreContext(options))
            {
                context.Database.EnsureCreated();
                context.SeedDatabaseFourBooks();

                var wrapped = context.SetupSingleDtoAndEntities<Tests.Dtos.ChangePubDateDto>();
                var service = new GenericServiceAsync(context, wrapped);

                //ATTEMPT
                var dto = new Tests.Dtos.ChangePubDateDto { BookId = 4, PublishedOn = new DateTime(2000,1,1) };
                await service.UpdateAndSaveAsync(dto);

                //VERIFY
                service.IsValid.ShouldBeTrue(service.GetAllErrors());
                service.Message.ShouldEqual("Successfully updated the Book");
                var entity = context.Books.Find(4);
                entity.PublishedOn.ShouldEqual(new DateTime(2000, 1, 1));
            }
        }

        [Fact]
        public async Task TestUpdatePublicationDateViaAutoMapperOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<EfCoreContext>();
            using (var context = new EfCoreContext(options))
            {
                context.Database.EnsureCreated();
                context.SeedDatabaseFourBooks();

                var wrapped = context.SetupSingleDtoAndEntities<Tests.Dtos.ChangePubDateDto>();
                var service = new GenericServiceAsync(context, wrapped);

                //ATTEMPT
                var dto = new Tests.Dtos.ChangePubDateDto { BookId = 4, PublishedOn = new DateTime(2000, 1, 1) };
                await service.UpdateAndSaveAsync(dto, "AutoMapper");

                //VERIFY
                service.IsValid.ShouldBeTrue(service.GetAllErrors());
                service.Message.ShouldEqual("Successfully updated the Book");
                var entity = context.Books.Find(4);
                entity.PublishedOn.ShouldEqual(new DateTime(2000, 1, 1));
            }
        }

        [Fact]
        public async Task TestUpdateViaStatedMethodOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<EfCoreContext>();
            using (var context = new EfCoreContext(options))
            {
                context.Database.EnsureCreated();
                context.SeedDatabaseFourBooks();

                var wrapped = context.SetupSingleDtoAndEntities<Tests.Dtos.ChangePubDateDto>();
                var service = new GenericServiceAsync(context, wrapped);

                //ATTEMPT
                var dto = new Tests.Dtos.ChangePubDateDto { BookId = 4, PublishedOn = new DateTime(2000, 1, 1) };
                await service.UpdateAndSaveAsync(dto, nameof(Book.RemovePromotion));

                //VERIFY
                service.IsValid.ShouldBeTrue(service.GetAllErrors());
                service.Message.ShouldEqual("Successfully updated the Book");
                var entity = context.Books.Find(4);
                entity.ActualPrice.ShouldEqual(220);
            }
        }

        [Fact]
        public async Task TestUpdateAddReviewOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<EfCoreContext>();
            using (var context = new EfCoreContext(options))
            {
                context.Database.EnsureCreated();
                context.SeedDatabaseFourBooks();

                //ATTEMPT
                var wrapped = context.SetupSingleDtoAndEntities<Tests.Dtos.AddReviewDto>();
                var service = new GenericServiceAsync(context, wrapped);

                //ATTEMPT
                var dto = new Tests.Dtos.AddReviewDto {BookId = 1, Comment = "comment", NumStars = 3, VoterName = "user" };
                await service.UpdateAndSaveAsync(dto, nameof(Book.AddReview));

                //VERIFY
                service.IsValid.ShouldBeTrue(service.GetAllErrors());
                service.Message.ShouldEqual("Successfully updated the Book");
                context.Set<Review>().Count().ShouldEqual(3);
            }
        }

        public class ConfigSettingMethod : PerDtoConfig<DtoWithConfig, Book>
        {
            public override string UpdateMethod { get; } = nameof(Book.RemovePromotion);
        }

        public class DtoWithConfig : ILinkToEntity<Book>
        {
            public int BookId { get; set; }
            public string Title { get; set; }
        }

        [Fact]
        public async Task TestUpdateViaStatedMethodInPerDtoConfigOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<EfCoreContext>();
            using (var context = new EfCoreContext(options))
            {
                context.Database.EnsureCreated();
                context.SeedDatabaseFourBooks();

                var wrapped = context.SetupSingleDtoAndEntities<DtoWithConfig>();
                var service = new GenericServiceAsync(context, wrapped);

                //ATTEMPT
                var dto = new DtoWithConfig { BookId = 4 };
                await service.UpdateAndSaveAsync(dto);

                //VERIFY
                service.IsValid.ShouldBeTrue(service.GetAllErrors());
                service.Message.ShouldEqual("Successfully updated the Book");
                var entity = context.Books.Find(4);
                entity.ActualPrice.ShouldEqual(220);
            }
        }

        [Fact]
        public async Task TestUpdateViaStatedMethodBad()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<EfCoreContext>();
            using (var context = new EfCoreContext(options))
            {
                context.Database.EnsureCreated();
                context.SeedDatabaseFourBooks();

                var wrapped = context.SetupSingleDtoAndEntities<Tests.Dtos.ChangePubDateDto>();
                var service = new GenericServiceAsync(context, wrapped);

                //ATTEMPT
                var dto = new Tests.Dtos.ChangePubDateDto { BookId = 4, PublishedOn = new DateTime(2000, 1, 1) };
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAndSaveAsync(dto, nameof(Book.AddReview)));

                //VERIFY
                ex.Message.ShouldStartWith("Could not find a method of name AddReview. The method that fit the properties in the DTO/VM are:");
            }
        }

        //[Fact]
        //public async Task TestUpdateViaAutoMapperBad()
        //{
        //    //SETUP
        //    var options = SqliteInMemory.CreateOptions<EfCoreContext>();
        //    using (var context = new EfCoreContext(options))
        //    {
        //        context.Database.EnsureCreated();
        //        context.SeedDatabaseFourBooks();

        //        var wrapped = context.SetupSingleDtoAndEntities<Tests.Dtos.ChangePubDateDto>(true);
        //        var service = new GenericServiceAsync(context, wrapped);

        //        //ATTEMPT
        //        var dto = new Tests.Dtos.ChangePubDateDto { BookId = 4, PublishedOn = new DateTime(2000, 1, 1) };
        //        var ex = Assert.Throws<InvalidOperationException>(() => await service.UpdateAndSaveAsync(dto, "AutoMapper"));

        //        //VERIFY
        //        ex.Message.ShouldStartWith("There was no way to update the entity class Book using AutoMapper.");
        //    }
        //}

    }
}