using Dapper;
using MySqlConnector;
using pizza_core_api.Core.Enums;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Services
{
    public class OrderService : IOrderService
    {
        protected readonly IConfiguration Configuration;
        private readonly ISessionService SessionService;

        // Единый набор JOIN-ов для получения заказов
        private const string OrderJoins = @"
                INNER JOIN user u ON o.user_id = u.id
                INNER JOIN adress a ON o.adress_id = a.id
                LEFT JOIN user au ON a.user_id = au.id
                LEFT JOIN order_pizza op ON o.id = op.order_id
                LEFT JOIN pizza p ON op.pizza_id = p.id
                LEFT JOIN user pu ON p.updated_by = pu.id
                LEFT JOIN pizza_ingredient pi ON p.id = pi.pizza_id
                LEFT JOIN ingredient bi ON pi.ingredient_id = bi.id AND bi.is_deleted = false
                LEFT JOIN order_pizza_ingredient opi ON op.id = opi.order_pizza_id
                LEFT JOIN ingredient i ON opi.ingredient_id = i.id
                LEFT JOIN user iu ON i.updated_by = iu.id";

        public OrderService(IConfiguration configuration, ISessionService sessionService)
        {
            Configuration = configuration;
            SessionService = sessionService;
        }

        // Единый набор колонок для получения заказов (используется и в GetOrderSql, и в GetOrdersPaged)
        private const string OrderSelectColumns = @"
                o.id as orderId,
                DATE_FORMAT(o.created_at, '%Y-%m-%dT%H:%i:%s') as orderCreatedAt,
                DATE_FORMAT(o.updated_at, '%Y-%m-%dT%H:%i:%s') as orderUpdatedAt,
                o.total_price as orderTotalPrice,
                o.status as orderStatus,
                o.payment_type as orderPaymentType,
                o.comment as orderComment,
                u.id as userId,
                u.name as userName,
                u.role as userType,
                a.id as adressId,
                a.city as adressCity,
                a.street as adressStreet,
                a.house as adressHouse,
                a.apartment as adressApartment,
                DATE_FORMAT(a.created_at, '%Y-%m-%dT%H:%i:%s') as adressCreatedAt,
                DATE_FORMAT(a.updated_at, '%Y-%m-%dT%H:%i:%s') as adressUpdatedAt,
                au.id as adressUserId,
                au.name as adressUserName,
                au.role as adressUserType,
                op.id as orderPizzaId,
                op.division_type as orderPizzaDivisionType,
                op.dough_type as orderPizzaDoughType,
                op.edge_type as orderPizzaEdgeType,
                op.total_price as orderPizzaTotalPrice,
                op.size as orderPizzaSize,
                DATE_FORMAT(op.created_at, '%Y-%m-%dT%H:%i:%s') as orderPizzaCreatedAt,
                p.id as pizzaId,
                p.type as pizzaType,
                p.size as pizzaSize,
                p.name as pizzaName,
                p.base_price as pizzaBasePrice,
                p.image_path as pizzaImagePath,
                p.is_deleted as pizzaIsDeleted,
                DATE_FORMAT(p.created_at, '%Y-%m-%dT%H:%i:%s') as pizzaCreatedAt,
                DATE_FORMAT(p.updated_at, '%Y-%m-%dT%H:%i:%s') as pizzaUpdatedAt,
                pu.id as pizzaUpdaterId,
                pu.name as pizzaUpdaterName,
                pu.role as pizzaUpdaterType,
                bi.id as pizzaIngredientId,
                bi.name as pizzaIngredientName,
                bi.type as pizzaIngredientType,
                opi.id as orderPizzaIngredientId,
                opi.total_price as orderPizzaIngredientTotalPrice,
                opi.quantity as orderPizzaIngredientQuantity,
                opi.action_type as orderPizzaIngredientActionType,
                DATE_FORMAT(opi.created_at, '%Y-%m-%dT%H:%i:%s') as orderPizzaIngredientCreatedAt,
                i.id as ingredientId,
                i.name as ingredientName,
                i.type as ingredientType,
                i.base_price as ingredientBasePrice,
                i.is_deleted as ingredientIsDeleted,
                DATE_FORMAT(i.created_at, '%Y-%m-%dT%H:%i:%s') as ingredientCreatedAt,
                DATE_FORMAT(i.updated_at, '%Y-%m-%dT%H:%i:%s') as ingredientUpdatedAt,
                iu.id as ingredientUpdaterId,
                iu.name as ingredientUpdaterName,
                iu.role as ingredientUpdaterType";

        public async Task<OrderResponse> GetOrder(long id, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                OrderResponse order = await GetOrderSql(id, connection);

                if (parsed.Value.userRole != UserRole.Admin.Code && parsed.Value.userId != order.user.id)
                    throw new InvalidOperationException("Данный заказ не принадлежит вам");

                return order;
            }
        }

        public async Task<OrderResponse> GetOrderSql(long id, MySqlConnection connection)
        {
            var sql = $@"
                SELECT
                    {OrderSelectColumns}
                FROM `order` o
                {OrderJoins}
                WHERE o.id = @id
                ORDER BY op.id ASC, opi.id ASC, bi.id ASC";

            var rows = (await connection.QueryAsync(sql, new { id })).ToList();

            var order = MapOrders(rows).FirstOrDefault();

            if (order is null)
                throw new KeyNotFoundException($"Заказ с идентификатором {id} не найден");

            return ToOrderResponse(order);
        }

        public async Task<OrdersResponse> GetOrdersPaged(OrderFilterRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                int offset = (request.pageNumber - 1) * request.pageSize;

                var whereConditions = new List<string>();
                var parameters = new DynamicParameters();

                if (!string.IsNullOrEmpty(request.status))
                {
                    whereConditions.Add("o2.status = @Status");
                    parameters.Add("Status", request.status);
                }

                if (!string.IsNullOrEmpty(request.paymentType))
                {
                    whereConditions.Add("o2.payment_type = @PaymentType");
                    parameters.Add("PaymentType", request.paymentType);
                }

                if (!string.IsNullOrEmpty(request.createdAtStart))
                {
                    whereConditions.Add("o2.created_at >= @CreatedAtStart");
                    parameters.Add("CreatedAtStart", request.createdAtStart);
                }

                if (!string.IsNullOrEmpty(request.createdAtEnd))
                {
                    whereConditions.Add("o2.created_at <= @CreatedAtEnd");
                    parameters.Add("CreatedAtEnd", request.createdAtEnd);
                }

                // Покупатель видит только свои заказы
                if (parsed.Value.userRole != UserRole.Admin.Code)
                {
                    whereConditions.Add("o2.user_id = @UserId");
                    parameters.Add("UserId", parsed.Value.userId);
                }

                string whereClause = whereConditions.Count > 0
                    ? $"WHERE {string.Join(" AND ", whereConditions)}"
                    : "";

                var validSortColumns = new Dictionary<string, string>
                {
                    ["id"] = "o2.id",
                    ["status"] = "o2.status",
                    ["paymenttype"] = "o2.payment_type",
                    ["totalprice"] = "o2.total_price",
                    ["createdat"] = "o2.created_at",
                    ["updatedat"] = "o2.updated_at"
                };

                var sortColumnKey = request.sortBy?.ToLower();
                string sortColumn = "o2.created_at";

                if (sortColumnKey is not null && validSortColumns.ContainsKey(sortColumnKey))
                    sortColumn = validSortColumns[sortColumnKey];

                string sortType = string.Equals(request.sortType, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

                // Один запрос: сначала выбираем страницу идентификаторов заказов (derived table),
                // затем одним JOIN-ом подтягиваем вложенные сущности (пиццы, ингредиенты и т.д.)
                string whereClausePage = whereConditions.Count > 0
                    ? $"WHERE {string.Join(" AND ", whereConditions).Replace("o2.", "o3.")}"
                    : "";

                string sortColumnPage = sortColumn.Replace("o2.", "o3.");

                var sql = $@"
                    SELECT
                        {OrderSelectColumns},
                        (SELECT COUNT(*) FROM `order` o2 {whereClause}) as totalCount
                    FROM `order` o
                    INNER JOIN (
                        SELECT
                            o3.id as id,
                            {sortColumnPage} as sortKey
                        FROM `order` o3
                        {whereClausePage}
                        ORDER BY {sortColumnPage} {sortType}, o3.id ASC
                        LIMIT @PageSize OFFSET @Offset
                    ) page ON page.id = o.id
                    {OrderJoins}
                    ORDER BY page.sortKey {sortType}, o.id ASC, op.id ASC, opi.id ASC, bi.id ASC";

                parameters.Add("PageSize", request.pageSize);
                parameters.Add("Offset", offset);

                var rows = (await connection.QueryAsync(sql, parameters)).ToList();

                int totalCount = rows.Count > 0 ? (int)(long)rows[0].totalCount : 0;
                int totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);

                var orders = MapOrders(rows);

                return new OrdersResponse(
                    orders,
                    totalCount,
                    request.pageNumber,
                    request.pageSize,
                    totalPages
                );
            }
        }

        // Собирает вложенную структуру заказов из плоских строк одного SQL-запроса
        private static List<Order> MapOrders(IEnumerable<dynamic> rows)
        {
            var orders = new Dictionary<long, Order>();
            var orderPizzas = new Dictionary<long, OrderPizza>();

            foreach (var row in rows)
            {
                long orderId = (long)row.orderId;

                if (!orders.TryGetValue(orderId, out var order))
                {
                    order = new Order
                    {
                        id = orderId,
                        user = new UserShort
                        {
                            id = (long)row.userId,
                            name = (string)row.userName,
                            type = (string)row.userType
                        },
                        adress = new Adress
                        {
                            id = (long)row.adressId,
                            user = new UserShort
                            {
                                id = (long)row.adressUserId,
                                name = (string)row.adressUserName,
                                type = (string)row.adressUserType
                            },
                            city = (string)row.adressCity,
                            street = (string)row.adressStreet,
                            house = (string)row.adressHouse,
                            apartment = (string?)row.adressApartment,
                            createdAt = (string)row.adressCreatedAt,
                            updatedAt = (string)row.adressUpdatedAt
                        },
                        orderPizzas = new List<OrderPizza>(),
                        totalPrice = (decimal)row.orderTotalPrice,
                        status = (string)row.orderStatus,
                        paymentType = (string)row.orderPaymentType,
                        comment = (string?)row.orderComment,
                        createdAt = (string)row.orderCreatedAt,
                        updatedAt = (string)row.orderUpdatedAt
                    };

                    orders.Add(orderId, order);
                }

                if (row.orderPizzaId is null)
                    continue;

                long orderPizzaId = (long)row.orderPizzaId;

                if (!orderPizzas.TryGetValue(orderPizzaId, out var orderPizza))
                {
                    orderPizza = new OrderPizza
                    {
                        id = orderPizzaId,
                        pizza = new Pizza
                        {
                            id = (long)row.pizzaId,
                            type = (string)row.pizzaType,
                            size = (string)row.pizzaSize,
                            name = (string)row.pizzaName,
                            basePrice = (decimal)row.pizzaBasePrice,
                            imagePath = (string)row.pizzaImagePath,
                            isDeleted = Convert.ToBoolean(row.pizzaIsDeleted),
                            createdAt = (string)row.pizzaCreatedAt,
                            updatedAt = (string)row.pizzaUpdatedAt,
                            updatedBy = row.pizzaUpdaterId is null ? null : new UserShort
                            {
                                id = (long)row.pizzaUpdaterId,
                                name = (string)row.pizzaUpdaterName,
                                type = (string)row.pizzaUpdaterType
                            },
                            ingredients = new List<IngredientShort>()
                        },
                        additionalIngredients = new List<AdditionalIngredient>(),
                        divisionType = (string)row.orderPizzaDivisionType,
                        doughType = (string)row.orderPizzaDoughType,
                        edgeType = (string)row.orderPizzaEdgeType,
                        totalPrice = (decimal)row.orderPizzaTotalPrice,
                        size = (string)row.orderPizzaSize,
                        createdAt = (string)row.orderPizzaCreatedAt
                    };

                    orderPizzas.Add(orderPizzaId, orderPizza);
                    order.orderPizzas.Add(orderPizza);
                }

                if (row.pizzaIngredientId is not null)
                {
                    long pizzaIngredientId = (long)row.pizzaIngredientId;

                    if (!orderPizza.pizza.ingredients.Any(x => x.id == pizzaIngredientId))
                    {
                        orderPizza.pizza.ingredients.Add(new IngredientShort
                        {
                            id = pizzaIngredientId,
                            name = (string)row.pizzaIngredientName,
                            type = (string)row.pizzaIngredientType
                        });
                    }
                }

                if (row.orderPizzaIngredientId is not null)
                {
                    long orderPizzaIngredientId = (long)row.orderPizzaIngredientId;

                    if (!orderPizza.additionalIngredients.Any(x => x.id == orderPizzaIngredientId))
                    {
                        orderPizza.additionalIngredients.Add(new AdditionalIngredient
                        {
                            id = orderPizzaIngredientId,
                            ingredient = new Ingredient
                            {
                                id = (long)row.ingredientId,
                                name = (string)row.ingredientName,
                                type = (string)row.ingredientType,
                                basePrice = (decimal)row.ingredientBasePrice,
                                isDeleted = Convert.ToBoolean(row.ingredientIsDeleted),
                                createdAt = (string)row.ingredientCreatedAt,
                                updatedAt = (string)row.ingredientUpdatedAt,
                                updatedBy = row.ingredientUpdaterId is null ? null : new UserShort
                                {
                                    id = (long)row.ingredientUpdaterId,
                                    name = (string)row.ingredientUpdaterName,
                                    type = (string)row.ingredientUpdaterType
                                }
                            },
                            totalPrice = (decimal)row.orderPizzaIngredientTotalPrice,
                            quantity = (decimal)row.orderPizzaIngredientQuantity,
                            actionType = (string)row.orderPizzaIngredientActionType,
                            createdAt = (string)row.orderPizzaIngredientCreatedAt
                        });
                    }
                }
            }

            return orders.Values.ToList();
        }

        private static OrderResponse ToOrderResponse(Order order)
        {
            return new OrderResponse(
                id: order.id,
                user: order.user,
                adress: order.adress,
                orderPizzas: order.orderPizzas,
                totalPrice: order.totalPrice,
                status: order.status,
                paymentType: order.paymentType,
                comment: order.comment,
                createdAt: order.createdAt,
                updatedAt: order.updatedAt
            );
        }

        public async Task<OrderResponse> CreateOrder(OrderCreateRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            if (request.orderPizzas is null || request.orderPizzas.Count == 0)
                throw new ArgumentException("Заказ должен содержать хотя бы одну пиццу");

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                long orderId;

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    // Проверяем адрес доставки
                    const string adressSql = "SELECT user_id FROM adress WHERE id = @AdressId";
                    long? adressOwnerId = await connection.ExecuteScalarAsync<long?>(adressSql, new { AdressId = request.adressId }, transaction);

                    if (adressOwnerId is null)
                        throw new KeyNotFoundException($"Адрес с идентификатором {request.adressId} не найден");

                    if (parsed.Value.userRole != UserRole.Admin.Code && adressOwnerId != parsed.Value.userId)
                        throw new InvalidOperationException("Данный адрес не принадлежит вам");

                    // Проверяем способ оплаты
                    string paymentType = string.IsNullOrEmpty(request.paymentType) ? PaymentType.Card.Code : request.paymentType;

                    if (paymentType != PaymentType.Card.Code && paymentType != PaymentType.Cash.Code)
                        throw new ArgumentException($"Недопустимый способ оплаты: {request.paymentType}");

                    // Создаём сам заказ (итоговую стоимость обновим после добавления пицц)
                    const string orderSql = @"
                        INSERT INTO `order` (user_id, adress_id, total_price, status, payment_type, comment)
                        VALUES (@UserId, @AdressId, 0, @Status, @PaymentType, @Comment);
                        SELECT LAST_INSERT_ID();";

                    orderId = await connection.ExecuteScalarAsync<long>(orderSql, new
                    {
                        UserId = parsed.Value.userId,
                        AdressId = request.adressId,
                        Status = OrderStatus.Created.Code,
                        PaymentType = paymentType,
                        Comment = request.comment
                    }, transaction);

                    decimal orderTotalPrice = 0;

                    foreach (var pizzaRequest in request.orderPizzas)
                    {
                        // Загружаем пиццу из меню
                        const string pizzaSql = "SELECT base_price FROM pizza WHERE id = @PizzaId AND is_deleted = false";
                        decimal? pizzaBasePrice = await connection.ExecuteScalarAsync<decimal?>(pizzaSql, new { PizzaId = pizzaRequest.pizzaId }, transaction);

                        if (pizzaBasePrice is null)
                            throw new KeyNotFoundException($"Пицца с идентификатором {pizzaRequest.pizzaId} не найдена или удалена");

                        if (pizzaRequest.divisionType != DivisionType.Whole.Code
                            && pizzaRequest.divisionType != DivisionType.Half.Code
                            && pizzaRequest.divisionType != DivisionType.Quarter.Code)
                            throw new ArgumentException($"Недопустимый тип разделения пиццы: {pizzaRequest.divisionType}");

                        if (pizzaRequest.doughType != DoughType.Classic.Code
                            && pizzaRequest.doughType != DoughType.Subtle.Code
                            && pizzaRequest.doughType != DoughType.Lush.Code)
                            throw new ArgumentException($"Недопустимый тип теста: {pizzaRequest.doughType}");

                        if (string.IsNullOrEmpty(pizzaRequest.edgeType))
                            throw new ArgumentException("Не указан тип борта пиццы");

                        double sizeMultiplier = Size.GetMultiplier(pizzaRequest.size);

                        if (sizeMultiplier < 0)
                            throw new ArgumentException($"Недопустимый размер пиццы: {pizzaRequest.size}");

                        decimal pizzaTotalPrice = Math.Round(pizzaBasePrice.Value * (decimal)sizeMultiplier, 2);

                        // Загружаем дополнительные ингредиенты
                        var additionalIngredientIds = pizzaRequest.additionalIngredientIds?.Distinct().ToList() ?? new List<long>();
                        var additionalIngredients = new List<(long id, decimal basePrice)>();

                        if (additionalIngredientIds.Count > 0)
                        {
                            const string ingredientSql = "SELECT id, base_price as basePrice FROM ingredient WHERE id IN @Ids AND is_deleted = false";
                            var ingredientRows = (await connection.QueryAsync(ingredientSql, new { Ids = additionalIngredientIds }, transaction)).ToList();

                            if (ingredientRows.Count != additionalIngredientIds.Count)
                                throw new KeyNotFoundException("Один или несколько дополнительных ингредиентов не найдены или удалены");

                            foreach (var ingredientRow in ingredientRows)
                            {
                                decimal ingredientBasePrice = (decimal)ingredientRow.basePrice;

                                pizzaTotalPrice = Math.Round(pizzaTotalPrice + ingredientBasePrice, 2);
                                additionalIngredients.Add(((long)ingredientRow.id, ingredientBasePrice));
                            }
                        }

                        // Добавляем пиццу в заказ
                        const string orderPizzaSql = @"
                            INSERT INTO order_pizza (pizza_id, order_id, division_type, dough_type, edge_type, total_price, size)
                            VALUES (@PizzaId, @OrderId, @DivisionType, @DoughType, @EdgeType, @TotalPrice, @Size);
                            SELECT LAST_INSERT_ID();";

                        long orderPizzaId = await connection.ExecuteScalarAsync<long>(orderPizzaSql, new
                        {
                            PizzaId = pizzaRequest.pizzaId,
                            OrderId = orderId,
                            DivisionType = pizzaRequest.divisionType,
                            DoughType = pizzaRequest.doughType,
                            EdgeType = pizzaRequest.edgeType,
                            TotalPrice = pizzaTotalPrice,
                            Size = pizzaRequest.size
                        }, transaction);

                        // Добавляем дополнительные ингредиенты пиццы в заказ
                        if (additionalIngredients.Count > 0)
                        {
                            const string orderPizzaIngredientSql = @"
                                INSERT INTO order_pizza_ingredient (order_pizza_id, ingredient_id, total_price, quantity, action_type)
                                VALUES (@OrderPizzaId, @IngredientId, @TotalPrice, @Quantity, @ActionType)";

                            foreach (var ingredient in additionalIngredients)
                            {
                                await connection.ExecuteAsync(orderPizzaIngredientSql, new
                                {
                                    OrderPizzaId = orderPizzaId,
                                    IngredientId = ingredient.id,
                                    TotalPrice = ingredient.basePrice,
                                    Quantity = 1.0m,
                                    ActionType = ActionType.Add.Code
                                }, transaction);
                            }
                        }

                        orderTotalPrice += pizzaTotalPrice;
                    }

                    // Обновляем итоговую стоимость заказа
                    const string updateTotalSql = "UPDATE `order` SET total_price = @TotalPrice WHERE id = @OrderId";
                    await connection.ExecuteAsync(updateTotalSql, new { TotalPrice = orderTotalPrice, OrderId = orderId }, transaction);

                    await transaction.CommitAsync();
                }

                return await GetOrderSql(orderId, connection);
            }
        }

        public async Task<OrderResponse> UpdateOrder(long id, OrderUpdateRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                const string ownerSql = "SELECT user_id FROM `order` WHERE id = @Id";
                long? ownerId = await connection.ExecuteScalarAsync<long?>(ownerSql, new { Id = id });

                if (ownerId is null)
                    throw new KeyNotFoundException($"Заказ с идентификатором {id} не найден");

                if (parsed.Value.userRole != UserRole.Admin.Code && ownerId != parsed.Value.userId)
                    throw new InvalidOperationException("Данный заказ не принадлежит вам");

                if (string.IsNullOrEmpty(request.status))
                    throw new ArgumentException("Статус заказа не может быть пустым");

                if (request.status != OrderStatus.Created.Code
                    && request.status != OrderStatus.Preparation.Code
                    && request.status != OrderStatus.Delivery.Code
                    && request.status != OrderStatus.Paid.Code
                    && request.status != OrderStatus.Canceled.Code)
                    throw new ArgumentException($"Недопустимый статус заказа: {request.status}");

                const string updateSql = @"
                    UPDATE `order`
                    SET status = @Status,
                        updated_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(updateSql, new { Status = request.status, Id = id });

                return await GetOrderSql(id, connection);
            }
        }
    }
}